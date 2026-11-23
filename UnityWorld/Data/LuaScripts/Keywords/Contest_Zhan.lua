local keyword = setmetatable({}, { __index = KeywordBase })

function keyword.OnResolveContest(card, ctx)
    if ctx:GetObject("IsWinner") and ctx:GetObject("WinnerTypeName") == "Zhan"
        and ctx:GetObject("LoserTypeName") == "Zhan" then
        ctx:Set("BaseDamage", ctx:GetObject("WinnerValue"))
    end
end

LuaMgr:RegisterKeyword("Zhan", keyword)
