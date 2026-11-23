local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if self.used or not CheckContestOutcome(ctx, self.m_Owner, "Lose") then return end
    if ctx:GetStringValue("LoserTypeName", "") ~= "Zhan" then return end
    self.used = true
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    AddArmor(source, "Self", 1)
end

return card
