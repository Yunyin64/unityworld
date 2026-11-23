local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    if self.m_Owner:GetHp() >= self.m_Owner:GetCombatHpMax() then return end
    Heal(ctx, "Self", 1)
    RemoveElementBuff(ctx, "Self", "None", true, 1)
end

return card
