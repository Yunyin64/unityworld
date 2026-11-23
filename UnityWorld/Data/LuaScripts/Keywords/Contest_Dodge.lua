local keyword = setmetatable({}, { __index = KeywordBase })

function keyword.OnResolveContest(card, ctx)
    if not ctx:GetObject("IsEnemy") then return end

    if ctx:GetObject("IsWinner") then
        if ctx:GetObject("WinnerTypeName") == "Dodge" and ctx:GetObject("LoserIsAttack") then
            Charge(ctx, "Self", ctx:GetObject("Overflow"))
        end
    elseif ctx:GetObject("LoserTypeName") == "Dodge" and ctx:GetObject("WinnerIsAttack") then
        ctx:Set("BaseDamage", ctx:GetObject("WinnerValue"))
    end
end

LuaMgr:RegisterKeyword("Dodge", keyword)
