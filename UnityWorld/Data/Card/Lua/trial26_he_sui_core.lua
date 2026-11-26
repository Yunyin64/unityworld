-- 错步身法。每轮给一件法宝供 1 灵。己方每次闪避赢过攻击拼点，回复 2 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetStringValue("WinnerTypeName", "") ~= "Dodge" then return end
    local rival = ctx:GetStringValue("LoserTypeName", "")
    if rival ~= "Zhan" and rival ~= "Ci" and rival ~= "Da" and rival ~= "SheJi" then return end
    RecoverMP(ctx, "Self", 2)
end

return card
