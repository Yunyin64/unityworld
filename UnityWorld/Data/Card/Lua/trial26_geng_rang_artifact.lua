-- 逆纹长刀。激活期间，本刀提供的攻击招式每次输掉实际配对，武器本轮攻击 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if not CheckContestOutcome(ctx, self.m_Owner, "Lose") then return end
    local kind = ctx:GetStringValue("LoserTypeName", "")
    if kind ~= "Zhan" and kind ~= "Ci" and kind ~= "Da" and kind ~= "SheJi" then return end
    local card = ctx:GetObject("LoserCard")
    if not HasKeyword(ctx, card, "ZhaoShi") or card.ParentCardId ~= self.m_Self.Id then return end
    local response = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    ChangeEquipmentStat(response, "Self", "Attack", 2, 1)
end

return card
