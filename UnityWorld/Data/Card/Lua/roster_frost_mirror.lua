local card = setmetatable({}, { __index = CardBase })

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.freezeRound == round or not CheckManaCharged(ctx, "Shui", "FaShu", true) then return end
    self.freezeRound = round
    Freeze(ctx, "TargetRandom", 0)
end

return card
