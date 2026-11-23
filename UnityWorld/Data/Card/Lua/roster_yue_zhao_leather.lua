local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if self.used or ctx.Caster.Team == self.m_Owner.Team or ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    self.used = true
    local relative = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    AddFortification(relative, "Self", 1)
end

return card
