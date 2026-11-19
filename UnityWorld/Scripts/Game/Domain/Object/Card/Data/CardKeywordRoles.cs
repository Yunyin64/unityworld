namespace UnityWorld.Game.Domain;

/// <summary>卡牌的核心运行方式。其余已注册 Keyword 都是普通词。</summary>
public enum CardMainKeyword
{
    FaShu,
    FaBao,
    Passive,
    ZhaoShi
}

public static class CardKeywordRoles
{
    private static readonly Dictionary<string, CardMainKeyword> MainKeywords =
        Enum.GetValues<CardMainKeyword>().ToDictionary(value => value.ToString(), StringComparer.Ordinal);

    internal static bool IsMain(string name) => MainKeywords.ContainsKey(name);

    // 模板预检需要在没有战斗 Lua 环境时取得与战斗一致的角色信息。
    public static bool TryGetMain(IEnumerable<string>? keywords, out CardMainKeyword main, out string error)
    {
        CardMainKeyword? found = null;
        if (keywords != null)
        {
            foreach (var keyword in keywords)
            {
                if (keyword == null || !MainKeywords.TryGetValue(keyword, out var role)) continue;
                if (found is { } previous && previous != role)
                {
                    main = default;
                    error = $"Main Keyword 冲突：{previous}、{role}。每张卡必须恰好有一个 Main。";
                    return false;
                }
                found = role;
            }
        }

        main = found.GetValueOrDefault();
        error = found.HasValue ? "" : "缺少 Main Keyword。每张卡必须恰好有一个 Main。";
        return found.HasValue;
    }

    internal static CardMainKeyword RequireMain(IEnumerable<string>? keywords, string? defineId = null)
    {
        if (TryGetMain(keywords, out var main, out var error)) return main;
        throw new InvalidOperationException(defineId == null ? error : $"卡 {defineId} {error}");
    }
}
