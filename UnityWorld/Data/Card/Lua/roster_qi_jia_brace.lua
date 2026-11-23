local card = setmetatable({}, { __index = CardBase })

function card:OnCardDisplaced(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    self.ready = true
end

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    if self.ready then
        defend = defend + 1
        self.ready = false
    end
    Defend(ctx, "Block", defend)
end

return card
