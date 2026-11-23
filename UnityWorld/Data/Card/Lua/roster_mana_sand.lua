local card = setmetatable({}, { __index = CardBase })

function card:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    Convert(ctx, "Self", "None", 1)
end

return card
