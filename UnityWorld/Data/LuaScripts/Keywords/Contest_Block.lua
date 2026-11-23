local keyword = setmetatable({}, { __index = KeywordBase })

function keyword.OnResolveContest(card, ctx)
    if ctx:GetObject("IsWinner") and ctx:GetObject("WinnerTypeName") == "Block"
        and ctx:GetObject("LoserIsAttack") and ctx:GetObject("IsEnemy") then
        AddNpcBuff(ctx, "Self", "Attack_Changed_Once", ctx:GetObject("Overflow"), -1)
    end
end

LuaMgr:RegisterKeyword("Block", keyword)
