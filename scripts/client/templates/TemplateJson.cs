using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xianxia.Client.Templates;

public static class TemplateJson
{
    public static readonly JsonSerializerOptions Options = Create(false);
    public static readonly JsonSerializerOptions StrictOptions = Create(true);
    private static JsonSerializerOptions Create(bool strict) => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = strict ? JsonUnmappedMemberHandling.Disallow : JsonUnmappedMemberHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;
}
