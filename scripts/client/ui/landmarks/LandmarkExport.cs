using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UnityWorld.Game.Data;

namespace Xianxia.Client.UI.Landmarks;

/// <summary>校验并写入地标定义。JSON 最后提交，读取方不会遇到尚未落盘的导入图。</summary>
internal static class LandmarkExport
{
    internal sealed record Source(string Id, string Path, string Content);

    private static readonly JsonSerializerOptions ExportOptions = new(MapCatalog.JsonOptions)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IgnoreReadOnlyProperties = true
    };

    internal static (Source Source, LandMarkDefine Draft) Open(MapCatalog catalog, string id)
    {
        string path = catalog.LandMarkSources[id];
        string content = File.ReadAllText(path);
        var records = JsonNode.Parse(content)!.AsArray();
        var draft = records[FindRecord(records, id)]!.Deserialize<LandMarkDefine>(MapCatalog.JsonOptions)!;
        return (new Source(id, path, content), draft);
    }

    internal static string Write(string dataRoot, string installRoot, LandMarkDefine draft, byte[]? image, Source? source = null)
    {
        if (source == null && !Regex.IsMatch(draft.ID, @"\A[a-z][a-z0-9_]{0,63}\z"))
            throw new InvalidDataException("ID 须以小写字母开头，只能含小写字母、数字和下划线，最多 64 字符。");
        if (source != null && draft.ID != source.Id) throw new InvalidDataException("已有地标的 ID 固定，请复制后使用新 ID。");
        if (string.IsNullOrWhiteSpace(draft.DisplayName)) throw new InvalidDataException("请输入地标名称。");

        string definitions = Path.Combine(dataRoot, "Data", "LandMarkDefines");
        Directory.CreateDirectory(definitions);
        // 同一安装的两个编辑器不能同时通过校验并发布相互冲突的地标。
        using var exportLock = new FileStream(Path.Combine(definitions, ".landmark-export.tmp"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        var catalog = MapCatalog.Load(dataRoot);

        string destination;
        if (source == null)
        {
            if (catalog.LandMarks.Keys.Contains(draft.ID, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException($"地标 ID 已存在：{draft.ID}");
            destination = Path.Combine(definitions, draft.ID + ".json");
            if (File.Exists(destination)) throw new IOException($"定义文件已存在：{destination}");
        }
        else
        {
            var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!catalog.LandMarkSources.TryGetValue(draft.ID, out var path) || !string.Equals(path, source.Path, pathComparison))
                throw new IOException("地标定义的来源已变更，请重新打开后编辑。");
            VerifySource(source);
            destination = source.Path;
        }

        string? imageDestination = null;
        if (image != null)
        {
            // 已有图片可能仍有其他定义引用，新图片用独立地址和本次修改一起提交。
            string filename = source == null ? draft.ID + ".png" : $"landmark_{Guid.NewGuid():N}.png";
            draft.ImagePath = $"landmarks/{filename}";
            imageDestination = Path.Combine(installRoot, "src", "landmarks", filename);
            if (File.Exists(imageDestination)) throw new IOException($"图片已存在：{draft.ImagePath}");
        }
        catalog.LandMarks[draft.ID] = draft;
        catalog.Validate();

        var record = JsonSerializer.SerializeToNode(draft, ExportOptions);
        var records = source == null ? new JsonArray(record) : JsonNode.Parse(source.Content)!.AsArray();
        if (source != null) records[FindRecord(records, source.Id)] = record;

        string token = "." + Guid.NewGuid().ToString("N") + ".tmp";
        string jsonTemp = destination + token;
        string? imageTemp = imageDestination == null ? null : imageDestination + token;
        bool imagePublished = false;
        try
        {
            File.WriteAllText(jsonTemp, records.ToJsonString(ExportOptions) + Environment.NewLine);
            if (imageDestination != null && imageTemp != null && image != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(imageDestination)!);
                File.WriteAllBytes(imageTemp, image);
                File.Move(imageTemp, imageDestination);
                imagePublished = true;
            }
            if (source != null) VerifySource(source);
            File.Move(jsonTemp, destination, overwrite: source != null);
            return destination;
        }
        catch
        {
            if (imagePublished) File.Delete(imageDestination!);
            throw;
        }
        finally
        {
            File.Delete(jsonTemp);
            if (imageTemp != null) File.Delete(imageTemp);
        }
    }

    private static int FindRecord(JsonArray records, string id)
    {
        for (int index = 0; index < records.Count; index++)
            if (records[index]?["ID"]?.GetValue<string>() == id) return index;
        throw new InvalidDataException($"原文件中找不到地标「{id}」，请重新加载目录。");
    }

    private static void VerifySource(Source source)
    {
        if (!File.Exists(source.Path) || File.ReadAllText(source.Path) != source.Content)
            throw new IOException("地标源文件在打开后已变更，请重新打开后编辑。");
    }
}
