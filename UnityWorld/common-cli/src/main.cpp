#include <iostream>
#include <string>
#include <vector>
#include <sstream>
#include <iomanip>
#include <optional>
#include <ctime>
#include <io.h>
#include <fcntl.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <bcrypt.h>
#include <shlobj.h>
#include <nlohmann/json.hpp>
#include <httplib.h>
#define PUGIXML_HEADER_ONLY
#include <pugixml/pugixml.hpp>

#pragma comment(lib, "bcrypt.lib")

std::string wstring_to_utf8(const std::wstring& wstr)
{
    if (wstr.empty())
        return {};
    int size = WideCharToMultiByte(CP_UTF8, 0, wstr.data(), (int)wstr.size(), NULL, 0, NULL, NULL);
    std::string result(size, 0);
    WideCharToMultiByte(CP_UTF8, 0, wstr.data(), (int)wstr.size(), &result[0], size, NULL, NULL);
    return result;
}

std::wstring utf8_to_wstring(const std::string& str)
{
    if (str.empty())
        return {};
    int size = MultiByteToWideChar(CP_UTF8, 0, str.data(), (int)str.size(), NULL, 0);
    std::wstring result(size, 0);
    MultiByteToWideChar(CP_UTF8, 0, str.data(), (int)str.size(), &result[0], size);
    return result;
}

std::string bytes_to_utf8(const std::string& bytes)
{
    if (bytes.empty())
        return {};
    // Try UTF-8 first
    int wsize = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, bytes.data(), (int)bytes.size(), NULL, 0);
    if (wsize > 0)
        return bytes; // Already valid UTF-8
    // Fall back to ACP
    wsize = MultiByteToWideChar(CP_ACP, 0, bytes.data(), (int)bytes.size(), NULL, 0);
    std::wstring wstr(wsize, 0);
    MultiByteToWideChar(CP_ACP, 0, bytes.data(), (int)bytes.size(), &wstr[0], wsize);
    return wstring_to_utf8(wstr);
}

std::string get_registry_path(const std::string& toolName)
{
    wchar_t* localAppData = nullptr;
    if (SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, NULL, &localAppData) != S_OK)
    {
        if (localAppData) CoTaskMemFree(localAppData);
        return "";
    }
    std::string base = wstring_to_utf8(localAppData);
    CoTaskMemFree(localAppData);
    return base + "\\common-cli\\" + toolName + "\\registry.json";
}

std::string read_file_locked(HANDLE hFile)
{
    LARGE_INTEGER fileSize;
    if (!GetFileSizeEx(hFile, &fileSize) || fileSize.QuadPart == 0)
        return "";

    std::string content((size_t)fileSize.QuadPart, '\0');
    DWORD bytesRead = 0;
    SetFilePointer(hFile, 0, NULL, FILE_BEGIN);
    ReadFile(hFile, &content[0], (DWORD)fileSize.QuadPart, &bytesRead, NULL);
    content.resize(bytesRead);
    return content;
}

void write_file_locked(HANDLE hFile, const std::string& content)
{
    SetFilePointer(hFile, 0, NULL, FILE_BEGIN);
    SetEndOfFile(hFile); // truncate
    DWORD bytesWritten = 0;
    WriteFile(hFile, content.c_str(), (DWORD)content.size(), &bytesWritten, NULL);
    FlushFileBuffers(hFile);
}

struct AuthCredential
{
    int revision = 0;
    std::string md5;
};

struct SvnLogInfo
{
    int revision = 0;
    std::string message;
    std::string md5;
};

std::string compute_md5_hex(const std::string& input)
{
    BCRYPT_ALG_HANDLE algorithm = nullptr;
    BCRYPT_HASH_HANDLE hash = nullptr;
    DWORD hashObjectSize = 0;
    DWORD dataSize = 0;
    DWORD hashSize = 0;

    if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_MD5_ALGORITHM, nullptr, 0) != 0)
        throw std::runtime_error("Failed to initialize MD5 provider");

    std::vector<UCHAR> hashObject;
    std::vector<UCHAR> hashBytes;

    if (BCryptGetProperty(algorithm, BCRYPT_OBJECT_LENGTH, reinterpret_cast<PUCHAR>(&hashObjectSize), sizeof(hashObjectSize), &dataSize, 0) != 0)
        goto cleanup;
    if (BCryptGetProperty(algorithm, BCRYPT_HASH_LENGTH, reinterpret_cast<PUCHAR>(&hashSize), sizeof(hashSize), &dataSize, 0) != 0)
        goto cleanup;

    hashObject.resize(hashObjectSize);
    hashBytes.resize(hashSize);

    if (BCryptCreateHash(algorithm, &hash, hashObject.data(), hashObjectSize, nullptr, 0, 0) != 0)
        goto cleanup;
    if (BCryptHashData(hash, reinterpret_cast<PUCHAR>(const_cast<char*>(input.data())), static_cast<ULONG>(input.size()), 0) != 0)
        goto cleanup;
    if (BCryptFinishHash(hash, hashBytes.data(), hashSize, 0) != 0)
        goto cleanup;

    {
        static const char* hex = "0123456789abcdef";
        std::string result;
        result.reserve(hashBytes.size() * 2);
        for (UCHAR byte : hashBytes)
        {
            result.push_back(hex[(byte >> 4) & 0x0F]);
            result.push_back(hex[byte & 0x0F]);
        }
        BCryptDestroyHash(hash);
        BCryptCloseAlgorithmProvider(algorithm, 0);
        return result;
    }

cleanup:
    if (hash)
        BCryptDestroyHash(hash);
    if (algorithm)
        BCryptCloseAlgorithmProvider(algorithm, 0);
    throw std::runtime_error("Failed to compute MD5");
}

std::string quote_windows_arg(const std::wstring& value)
{
    std::wstring quoted = L"\"";
    for (wchar_t ch : value)
    {
        if (ch == L'\"')
            quoted += L'\\';
        quoted += ch;
    }
    quoted += L"\"";
    return wstring_to_utf8(quoted);
}

void read_pipe_to_string(HANDLE handle, std::string& output)
{
    char buffer[4096];
    DWORD bytesRead = 0;
    while (ReadFile(handle, buffer, sizeof(buffer), &bytesRead, NULL) && bytesRead > 0)
    {
        output.append(buffer, bytesRead);
    }
}

SvnLogInfo get_latest_svn_log(const std::string& svnUrl)
{
    SECURITY_ATTRIBUTES sa = {};
    sa.nLength = sizeof(sa);
    sa.bInheritHandle = TRUE;

    HANDLE stdoutRead = NULL;
    HANDLE stdoutWrite = NULL;
    HANDLE stderrRead = NULL;
    HANDLE stderrWrite = NULL;

    if (!CreatePipe(&stdoutRead, &stdoutWrite, &sa, 0) || !CreatePipe(&stderrRead, &stderrWrite, &sa, 0))
        throw std::runtime_error("Failed to create pipes for svn process");

    SetHandleInformation(stdoutRead, HANDLE_FLAG_INHERIT, 0);
    SetHandleInformation(stderrRead, HANDLE_FLAG_INHERIT, 0);

    std::wstring command = utf8_to_wstring("svn log --xml --limit 1 " + quote_windows_arg(utf8_to_wstring(svnUrl)));
    std::vector<wchar_t> commandLine(command.begin(), command.end());
    commandLine.push_back(L'\0');

    STARTUPINFOW si = {};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
    si.hStdOutput = stdoutWrite;
    si.hStdError = stderrWrite;

    PROCESS_INFORMATION pi = {};
    if (!CreateProcessW(NULL, commandLine.data(), NULL, NULL, TRUE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
    {
        CloseHandle(stdoutRead);
        CloseHandle(stdoutWrite);
        CloseHandle(stderrRead);
        CloseHandle(stderrWrite);
        throw std::runtime_error("Failed to start svn process");
    }

    CloseHandle(stdoutWrite);
    CloseHandle(stderrWrite);

    WaitForSingleObject(pi.hProcess, INFINITE);

    std::string stdoutText;
    std::string stderrText;
    read_pipe_to_string(stdoutRead, stdoutText);
    read_pipe_to_string(stderrRead, stderrText);

    DWORD exitCode = 1;
    GetExitCodeProcess(pi.hProcess, &exitCode);
    CloseHandle(stdoutRead);
    CloseHandle(stderrRead);
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

    if (exitCode != 0)
    {
        throw std::runtime_error(stderrText.empty() ? "svn log failed" : stderrText);
    }

    pugi::xml_document doc;
    pugi::xml_parse_result parseResult = doc.load_string(stdoutText.c_str());
    if (!parseResult)
        throw std::runtime_error(std::string("svn log XML parse failed: ") + parseResult.description());

    pugi::xml_node logentry = doc.child("log").child("logentry");
    if (!logentry)
        throw std::runtime_error("svn log XML missing logentry");

    pugi::xml_attribute revisionAttr = logentry.attribute("revision");
    if (!revisionAttr)
        throw std::runtime_error("svn log XML missing revision");

    std::string message = logentry.child_value("msg");

    SvnLogInfo info;
    info.revision = revisionAttr.as_int();
    info.message = message;
    info.md5 = compute_md5_hex(message);
    return info;
}

std::wstring get_auth_cache_path()
{
    wchar_t* localAppData = nullptr;
    if (SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, NULL, &localAppData) != S_OK)
    {
        if (localAppData) CoTaskMemFree(localAppData);
        throw std::runtime_error("Failed to locate LOCALAPPDATA");
    }

    std::wstring base(localAppData);
    CoTaskMemFree(localAppData);
    std::wstring dir = base + L"\\common-cli";
    CreateDirectoryW(dir.c_str(), NULL);
    return dir + L"\\auth_cache.json";
}

HANDLE open_auth_cache_file(OVERLAPPED& ov)
{
    std::wstring cachePath = get_auth_cache_path();
    HANDLE hFile = CreateFileW(
        cachePath.c_str(),
        GENERIC_READ | GENERIC_WRITE,
        0,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        return INVALID_HANDLE_VALUE;
    if (!LockFileEx(hFile, LOCKFILE_EXCLUSIVE_LOCK, 0, MAXDWORD, MAXDWORD, &ov))
    {
        CloseHandle(hFile);
        return INVALID_HANDLE_VALUE;
    }
    return hFile;
}

bool try_parse_time_utc(const std::string& text, std::time_t& value)
{
    std::tm tm = {};
    std::istringstream stream(text);
    stream >> std::get_time(&tm, "%Y-%m-%dT%H:%M:%SZ");
    if (stream.fail())
        return false;
    value = _mkgmtime(&tm);
    return value != -1;
}

std::string format_time_utc(std::time_t value)
{
    std::tm tm = {};
    gmtime_s(&tm, &value);
    std::ostringstream stream;
    stream << std::put_time(&tm, "%Y-%m-%dT%H:%M:%SZ");
    return stream.str();
}

nlohmann::json read_auth_cache_json(HANDLE hFile)
{
    std::string content = read_file_locked(hFile);
    if (content.empty())
        return nlohmann::json::object();
    try
    {
        nlohmann::json json = nlohmann::json::parse(content);
        if (json.is_object())
            return json;
    }
    catch (const nlohmann::json::parse_error&)
    {
    }
    return nlohmann::json::object();
}

bool try_get_cached_auth(const std::string& requestKey, AuthCredential& credential)
{
    OVERLAPPED ov = {};
    HANDLE hFile = open_auth_cache_file(ov);
    if (hFile == INVALID_HANDLE_VALUE)
        return false;

    nlohmann::json cache = read_auth_cache_json(hFile);
    bool found = false;
    if (cache.contains(requestKey) && cache[requestKey].is_object())
    {
        const auto& entry = cache[requestKey];
        std::string expiresAt = entry.value("expires_at", std::string(""));
        std::time_t expiresTime = 0;
        if (try_parse_time_utc(expiresAt, expiresTime) && expiresTime > std::time(nullptr))
        {
            credential.revision = entry.value("revision", 0);
            credential.md5 = entry.value("md5", std::string(""));
            found = credential.revision > 0 && !credential.md5.empty();
        }
    }

    UnlockFileEx(hFile, 0, MAXDWORD, MAXDWORD, &ov);
    CloseHandle(hFile);
    return found;
}

void write_cached_auth(const std::string& requestKey, const AuthCredential& credential)
{
    OVERLAPPED ov = {};
    HANDLE hFile = open_auth_cache_file(ov);
    if (hFile == INVALID_HANDLE_VALUE)
        throw std::runtime_error("Failed to open auth cache file");

    nlohmann::json cache = read_auth_cache_json(hFile);
    cache[requestKey] = {
        { "revision", credential.revision },
        { "md5", credential.md5 },
        { "expires_at", format_time_utc(std::time(nullptr) + 24 * 60 * 60) },
    };
    write_file_locked(hFile, cache.dump(2));

    UnlockFileEx(hFile, 0, MAXDWORD, MAXDWORD, &ov);
    CloseHandle(hFile);
}

std::string get_current_username()
{
    wchar_t buffer[256];
    DWORD size = 256;
    if (!GetUserNameW(buffer, &size))
        return "";
    return wstring_to_utf8(std::wstring(buffer, size - 1));
}

std::string build_request_body(const std::vector<std::string>& args, const std::optional<AuthCredential>& auth, const std::string& user)
{
    nlohmann::json req;
    req["user"] = user;
    req["args"] = args;
    if (auth.has_value())
    {
        req["auth"] = {
            { "revision", auth->revision },
            { "md5", auth->md5 },
        };
    }
    return req.dump();
}

bool try_parse_auth_required_response(const std::string& body, std::string& svnUrl)
{
    try
    {
        nlohmann::json json = nlohmann::json::parse(body);
        if (json.value("error", std::string("")) != "auth_required")
            return false;
        svnUrl = json.value("svn_url", std::string(""));
        return !svnUrl.empty();
    }
    catch (const nlohmann::json::parse_error&)
    {
        return false;
    }
}

nlohmann::json get_alive_workspaces(const std::string& toolName)
{
    nlohmann::json alive = nlohmann::json::array();

    std::string registryPath = get_registry_path(toolName);
    if (registryPath.empty())
        return alive;

    // Open registry file (read-write for potential cleanup)
    HANDLE hFile = CreateFileA(
        registryPath.c_str(),
        GENERIC_READ | GENERIC_WRITE,
        0,        // no sharing — exclusive access
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if (hFile == INVALID_HANDLE_VALUE)
        return alive; // file doesn't exist yet — no workspaces registered

    // Acquire file lock
    OVERLAPPED ov = {};
    if (!LockFileEx(hFile, LOCKFILE_EXCLUSIVE_LOCK, 0, MAXDWORD, MAXDWORD, &ov))
    {
        CloseHandle(hFile);
        return alive;
    }

    // Read and parse JSON
    std::string content = read_file_locked(hFile);
    nlohmann::json data;
    try
    {
        if (!content.empty())
            data = nlohmann::json::parse(content);
    }
    catch (const nlohmann::json::parse_error&)
    {
    }

    if (!data.contains("workspaces") || !data["workspaces"].is_array())
    {
        if (!content.empty())
            std::cerr << "Warning: registry file corrupted or has unexpected format: " << registryPath << std::endl;
        UnlockFileEx(hFile, 0, MAXDWORD, MAXDWORD, &ov);
        CloseHandle(hFile);
        return alive;
    }

    // Filter alive workspaces via CreateFileA pipe probe
    bool hasDead = false;

    for (const auto& ws : data["workspaces"])
    {
        std::string id = ws.value("id", std::string(""));
        if (id.empty())
        {
            hasDead = true;
            continue;
        }

        std::string pipeName = "\\\\.\\pipe\\common-cli-" + toolName + "-" + id;
        HANDLE hProbe = CreateFileA(
            pipeName.c_str(),
            GENERIC_READ | GENERIC_WRITE,
            0,
            NULL,
            OPEN_EXISTING,
            0,
            NULL);

        if (hProbe != INVALID_HANDLE_VALUE)
        {
            // Pipe exists and is connectable — workspace is alive
            CloseHandle(hProbe);
            alive.push_back(ws);
        }
        else
        {
            DWORD err = GetLastError();
            if (err == ERROR_PIPE_BUSY)
            {
                // Pipe exists but all instances are busy — still alive
                alive.push_back(ws);
            }
            else
            {
                // Pipe does not exist — workspace is dead
                hasDead = true;
            }
        }
    }

    // Write back cleaned JSON if dead entries were found
    if (hasDead)
    {
        nlohmann::json cleaned;
        cleaned["workspaces"] = alive;
        std::string cleanedStr = cleaned.dump();
        write_file_locked(hFile, cleanedStr);
    }

    // Release file lock and close
    UnlockFileEx(hFile, 0, MAXDWORD, MAXDWORD, &ov);
    CloseHandle(hFile);

    return alive;
}

int wmain(int argc, wchar_t* argv[])
{
    SetConsoleOutputCP(CP_UTF8);
    _setmode(_fileno(stdout), _O_BINARY);

    if (argc < 2)
    {
        std::cerr << "Usage: common-cli.exe <ToolName|http://url> [args...]" << std::endl;
        return 1;
    }

    std::string toolName = wstring_to_utf8(argv[1]);

    // --- HTTPS: not supported ---
    if (toolName.rfind("https://", 0) == 0)
    {
        std::cerr << "HTTPS is not supported, use http://" << std::endl;
        return 1;
    }

    // --- HTTP mode ---
    if (toolName.rfind("http://", 0) == 0)
    {
        // Collect arguments: argv[2..] (no WorkspaceId in HTTP mode)
        std::vector<std::string> args;
        for (int i = 2; i < argc; i++)
        {
            args.push_back(wstring_to_utf8(argv[i]));
        }

        // Detect stdin (same logic as pipe mode)
        HANDLE hStdin = GetStdHandle(STD_INPUT_HANDLE);
        DWORD stdinType = GetFileType(hStdin);
        bool hasStdin = false;
        if (stdinType == FILE_TYPE_DISK)
        {
            hasStdin = true;
        }
        else if (stdinType == FILE_TYPE_PIPE)
        {
            DWORD bytesAvail = 0;
            PeekNamedPipe(hStdin, NULL, 0, NULL, &bytesAvail, NULL);
            hasStdin = (bytesAvail > 0);
        }
        if (hasStdin)
        {
            std::ostringstream ss;
            ss << std::cin.rdbuf();
            args.push_back(bytes_to_utf8(ss.str()));
        }

        // Parse URL: http://host:port/path
        std::string url = toolName;
        std::string hostPort = url.substr(7); // skip "http://"
        std::string path = "/";

        size_t slashPos = hostPort.find('/');
        if (slashPos != std::string::npos)
        {
            path = hostPort.substr(slashPos);
            hostPort = hostPort.substr(0, slashPos);
        }

        std::string host = hostPort;
        int port = 80;

        size_t colonPos = hostPort.find(':');
        if (colonPos != std::string::npos)
        {
            host = hostPort.substr(0, colonPos);
            port = std::stoi(hostPort.substr(colonPos + 1));
        }

        std::string requestKey = hostPort + path;
        std::optional<AuthCredential> auth;
        AuthCredential cachedAuth;
        if (try_get_cached_auth(requestKey, cachedAuth))
        {
            auth = cachedAuth;
        }

        httplib::Client client(host, port);
        std::string username = get_current_username();
        std::string body = build_request_body(args, auth, username);
        auto res = client.Post(path, body, "application/json");

        if (!res)
        {
            std::cerr << "Failed to connect to " << url << std::endl;
            return 1;
        }

        bool usedAuth = auth.has_value();

        if (res->status == 401)
        {
            std::string svnUrl;
            if (!try_parse_auth_required_response(res->body, svnUrl))
            {
                std::cerr << res->body;
                return 1;
            }

            try
            {
                SvnLogInfo latestLog = get_latest_svn_log(svnUrl);
                auth = AuthCredential{ latestLog.revision, latestLog.md5 };
                usedAuth = true;
                body = build_request_body(args, auth, username);
                res = client.Post(path, body, "application/json");
            }
            catch (const std::exception& e)
            {
                std::cerr << e.what() << std::endl;
                return 1;
            }

            if (!res)
            {
                std::cerr << "Failed to connect to " << url << std::endl;
                return 1;
            }

            if (res->status == 401)
            {
                std::cerr << res->body;
                return 1;
            }
        }

        if (res->status >= 200 && res->status < 300)
        {
            if (usedAuth && auth.has_value())
            {
                try
                {
                    write_cached_auth(requestKey, *auth);
                }
                catch (const std::exception& e)
                {
                    std::cerr << e.what() << std::endl;
                    return 1;
                }
            }
            std::cout << res->body;
            return 0;
        }
        else
        {
            std::cerr << res->body;
            return 1;
        }
    }

    // --- Pipe mode (original behavior) ---
    if (argc < 3)
    {
        std::cerr << "Usage: common-cli.exe <ToolName> <WorkspaceId|auto|list> [args...]" << std::endl;
        return 1;
    }

    std::string workspaceId = wstring_to_utf8(argv[2]);

    // List alive workspaces
    if (workspaceId == "list")
    {
        nlohmann::json alive = get_alive_workspaces(toolName);
        nlohmann::json result;
        result["workspaces"] = alive;
        std::cout << result.dump() << std::endl;
        return 0;
    }

    // Auto-resolve workspace
    if (workspaceId == "auto")
    {
        nlohmann::json alive = get_alive_workspaces(toolName);

        if (alive.size() == 0)
        {
            nlohmann::json result;
            result["workspaces"] = alive;
            result["message"] = "没有活动的工作区，请打开" + toolName + "工具";
            std::cout << result.dump() << std::endl;
            return 1;
        }

        if (alive.size() > 1)
        {
            nlohmann::json result;
            result["workspaces"] = alive;
            result["message"] = "存在多个工作区，请询问用户要连接哪个工作区";
            std::cout << result.dump() << std::endl;
            return 1;
        }

        // Exactly one workspace — use it
        workspaceId = alive[0].value("id", std::string(""));
    }

    // Collect remaining arguments (argv[3..])
    std::vector<std::string> args;
    for (int i = 3; i < argc; i++)
    {
        args.push_back(wstring_to_utf8(argv[i]));
    }

    // Detect stdin: use GetFileType + PeekNamedPipe
    HANDLE hStdin = GetStdHandle(STD_INPUT_HANDLE);
    DWORD stdinType = GetFileType(hStdin);
    bool hasStdin = false;
    if (stdinType == FILE_TYPE_DISK)
    {
        hasStdin = true;
    }
    else if (stdinType == FILE_TYPE_PIPE)
    {
        DWORD bytesAvail = 0;
        PeekNamedPipe(hStdin, NULL, 0, NULL, &bytesAvail, NULL);
        hasStdin = (bytesAvail > 0);
    }
    if (hasStdin)
    {
        std::ostringstream ss;
        ss << std::cin.rdbuf();
        args.push_back(bytes_to_utf8(ss.str()));
    }

    // Serialize as JSON request
    nlohmann::json req;
    req["args"] = args;
    std::string reqStr = req.dump();

    // 3.1 Compute pipe name: \\.\pipe\common-cli-{ToolName}-{WorkspaceId}
    std::string pipeName = "\\\\.\\pipe\\common-cli-" + toolName + "-" + workspaceId;

    // Connect to named pipe
    HANDLE hPipe = CreateFileA(
        pipeName.c_str(),
        GENERIC_READ | GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        0,
        NULL);

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        std::cerr << "Failed to connect to tool '" << toolName << "' workspace '" << workspaceId << "' (pipe: " << pipeName << ")" << std::endl;
        return 1;
    }

    // Send request (JSON + newline)
    reqStr += "\n";
    DWORD bytesWritten;
    if (!WriteFile(hPipe, reqStr.c_str(), (DWORD)reqStr.size(), &bytesWritten, NULL))
    {
        std::cerr << "Failed to send request to tool '" << toolName << "' workspace '" << workspaceId << "'" << std::endl;
        CloseHandle(hPipe);
        return 1;
    }
    FlushFileBuffers(hPipe);

    // Read response until newline
    std::string response;
    char ch;
    DWORD bytesRead;
    while (true)
    {
        if (!ReadFile(hPipe, &ch, 1, &bytesRead, NULL) || bytesRead == 0)
        {
            std::cerr << "Pipe connection lost while waiting for response from tool '" << toolName << "' workspace '" << workspaceId << "'" << std::endl;
            CloseHandle(hPipe);
            return 1;
        }
        if (ch == '\n')
            break;
        response += ch;
    }
    CloseHandle(hPipe);

    // Parse response JSON
    nlohmann::json resp;
    try
    {
        resp = nlohmann::json::parse(response);
    }
    catch (const nlohmann::json::parse_error&)
    {
        std::cerr << "Invalid response from tool: " << response << std::endl;
        return 1;
    }

    int code = resp.value("code", 1);
    std::string output = resp.value("output", std::string(""));
    std::string error = resp.value("error", std::string(""));

    if (!output.empty())
        std::cout << output;
    if (!error.empty())
        std::cerr << error;

    return code;
}
