namespace UnityWorld.Game.Domain;

/// <summary>卡牌的核心运行方式。其余已注册 Keyword 都是普通词。</summary>
public enum CardMainKeyword
{
    /// <summary>法术主关键词</summary>
    FaShu,
    /// <summary>法宝主关键词</summary>
    FaBao,
    /// <summary>被动主关键词</summary>
    Passive,
    /// <summary>招式主关键词</summary>
    ZhaoShi
}

public static class CardKeywordRoles
{
    /// <summary>主关键词名到枚举值的映射</summary>
    private static readonly Dictionary<string, CardMainKeyword> MainKeywords =
        Enum.GetValues<CardMainKeyword>().ToDictionary(value => value.ToString(), StringComparer.Ordinal);

    /// <summary>检查名称是否属于主关键词</summary>
    internal static bool IsMain(string name) => MainKeywords.ContainsKey(name);

    // 模板预检需要在没有战斗 Lua 环境时取得与战斗一致的角色信息。
    /// <summary>解析唯一主关键词，缺失或冲突时返回错误</summary>
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

    /// <summary>取得唯一主关键词，无效时抛出含定义编号的异常</summary>
    internal static CardMainKeyword RequireMain(IEnumerable<string>? keywords, string? defineId = null)
    {
        if (TryGetMain(keywords, out var main, out var error)) return main;
        throw new InvalidOperationException(defineId == null ? error : $"卡 {defineId} {error}");
    }
}
