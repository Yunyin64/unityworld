local card = setmetatable({}, { __index = CardBase })

function card:OnWoundGained(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used then return end
    self.used = true
    RecoverMP(ctx, "Self", 3)
end

return card
