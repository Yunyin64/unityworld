local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local moved = ctx:GetObject("MovedCard")
    if moved == nil then return end
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    source.CardTargets:Add(moved)
    RootCard(source, "Selected", 2)
end

return card
