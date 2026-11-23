local card = setmetatable({}, { __index = CardBase })

function card:OnShieldChargeReady(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used then return end
    self.used = true
    RecoverMP(ctx, "Self", 2)
end

return card
