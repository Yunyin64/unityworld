local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster == nil or ctx.Caster.Team == self.m_Owner.Team then return end
    if ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    if self.used then return end
    self.used = true
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    RecoverMP(source, "Self", 2)
end

return card
