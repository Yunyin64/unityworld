local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.shieldRound == round or not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if 2 * self.m_Owner:GetHp() > self.m_Owner:GetCombatHpMax() or self.m_Owner.ShieldValue >= 2 then return end
    self.shieldRound = round
    GainShield(ctx, "Self", 1)
end

return card
