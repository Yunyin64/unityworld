local keyword = setmetatable({}, { __index = KeywordBase })

function keyword.OnResolveContest(card, ctx)
    if ctx:GetObject("IsWinner") and ctx:GetObject("WinnerTypeName") == "Ci"
        and ctx:GetObject("LoserTypeName") == "Ci" then
        ctx:Set("BaseDamage", ctx:GetObject("WinnerValue"))
    end
end

LuaMgr:RegisterKeyword("Ci", keyword)
