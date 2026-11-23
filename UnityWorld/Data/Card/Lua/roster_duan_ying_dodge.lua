local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    Defend(ctx, "Dodge", defend)
end

function card:Apply(ctx)
    if self.m_Owner.ShieldValue > 0 then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round then return end
    self.rewardedRound = round
    AddArmor(ctx, "Self", 1)
end

return card
