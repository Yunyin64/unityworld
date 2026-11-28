using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using UnityWorld.Game.Data;

namespace Xianxia.Client.UI.Regions;

/// <summary>按来源文件保存模板，并用正式地图目录预检每次修改。</summary>
internal sealed class RegionTemplateStore(string root)
{
    internal sealed record Source(string Id, string Path, string Content);

    internal static readonly JsonSerializerOptions DocumentOptions = new(MapCatalog.JsonOptions)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string dataRoot = Path.GetFullPath(Path.Combine(root, "Data"));

    internal MapCatalog Load() => MapCatalog.Load(dataRoot);

    internal static (Source Source, JsonObject Definition) Open(MapCatalog catalog, string id)
    {
        string path = catalog.RegionSources[id];
        var source = new Source(id, path, File.ReadAllText(path));
        var records = ParseRecords(source.Content);
        return (source, (JsonObject)records[FindRecord(records, id)]!.DeepClone());
    }

    internal (MapCatalog Catalog, string Path, string Content) Save(Source? source, JsonObject definition)
    {
        string id = definition["ID"]!.GetValue<string>();
        if (source != null && id != source.Id)
            throw new InvalidDataException("已有模板的 ID 固定。请复制后以新 ID 保存。");
        return WithWriteLock(() =>
        {
            string path;
            JsonArray records;
            if (source == null)
            {
                if (!Regex.IsMatch(id, @"\A[A-Za-z0-9][A-Za-z0-9_-]{0,79}\z"))
                    throw new InvalidDataException("新模板 ID 使用 1–80 个英文字母、数字、下划线或连字符，并以字母或数字开头。");
                if (Load().Regions.Keys.Contains(id, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException($"区域 ID「{id}」已被占用。");
                path = Path.Combine(dataRoot, "RegionDefines", id + ".json");
                if (File.Exists(path)) throw new IOException($"文件已存在：{path}");
                records = new JsonArray(definition.DeepClone());
            }
            else
            {
                VerifySource(source);
                path = source.Path;
                records = ParseRecords(source.Content);
                records[FindRecord(records, id)] = definition.DeepClone();
            }

            string content = records.ToJsonString(DocumentOptions) + Environment.NewLine;
            var catalog = MapCatalog.PreviewRegionSource(dataRoot, path, content);
            Write(path, content, source);
            return (catalog, path, content);
        });
    }

    internal string Archive(Source source) => WithWriteLock(() =>
    {
        VerifySource(source);
        var records = ParseRecords(source.Content);
        records.RemoveAt(FindRecord(records, source.Id));
        string? content = records.Count == 0 ? null : records.ToJsonString(DocumentOptions) + Environment.NewLine;
        MapCatalog.PreviewRegionSource(dataRoot, source.Path, content);
        string backup = $"{source.Path}.{Guid.NewGuid():N}.bak";
        VerifySource(source);
        if (content == null)
            File.Move(source.Path, backup);
        else
        {
            File.Copy(source.Path, backup);
            Write(source.Path, content, source);
        }
        return backup;
    });

    private static JsonArray ParseRecords(string json) => JsonNode.Parse(json)!.AsArray();

    private static int FindRecord(JsonArray records, string id)
    {
        for (int index = 0; index < records.Count; index++)
            if (records[index]?["ID"]?.GetValue<string>() == id) return index;
        throw new InvalidDataException($"原文件已找不到模板「{id}」，请重新读取。");
    }

    private static void VerifySource(Source source)
    {
        if (!File.Exists(source.Path) || File.ReadAllText(source.Path) != source.Content)
            throw new IOException($"源文件在打开后已变更，请重新读取后再编辑：{source.Path}");
    }

    private static void Write(string path, string content, Source? source)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, content);
            // 与目标同目录，读者看到旧版或完整新版。新建时保留独占语义。
            if (source != null) VerifySource(source);
            File.Move(temporary, path, overwrite: source != null);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private T WithWriteLock<T>(Func<T> operation)
    {
        string key = OperatingSystem.IsWindows() ? dataRoot.ToUpperInvariant() : dataRoot;
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        using var mutex = new Mutex(false, "UnityWorld.RegionTemplates." + hash);
        try
        {
            if (!mutex.WaitOne(0)) throw new IOException("另一个区域编辑器正在保存，请重试。");
        }
        catch (AbandonedMutexException)
        {
            // 上次进程已退出。写入从重新读取和校验源文件开始。
        }
        try { return operation(); }
        finally { mutex.ReleaseMutex(); }
    }
}
