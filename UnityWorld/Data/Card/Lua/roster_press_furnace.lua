local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Huo", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
    GainShield(ctx, "Self", 2)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckManaUsed(ctx, "Huo", 2) then return end
    self.used = true
    if self.m_Owner.ShieldValue < 2 then GainShield(ctx, "Self", 1) end
end

return card
