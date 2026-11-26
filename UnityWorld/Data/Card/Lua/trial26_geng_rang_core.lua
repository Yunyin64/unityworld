-- 让刃诀。每轮为法宝供 1 灵。己方攻击每次以 1 或 2 点差输掉实际配对，获得 2 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnContestOverflow(ctx)
    if not CheckContestOutcome(ctx, self.m_Owner, "Lose") then return end
    local kind = ctx:GetStringValue("LoserTypeName", "")
    if kind ~= "Zhan" and kind ~= "Ci" and kind ~= "Da" and kind ~= "SheJi" then return end
    if ctx:GetObject("Overflow") > 2 then return end
    local response = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    AddArmor(response, "Self", 2)
end

return card
