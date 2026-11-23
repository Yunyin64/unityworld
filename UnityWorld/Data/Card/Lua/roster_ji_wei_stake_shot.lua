local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    -- Pinning the opening remembers one displacement until the next shot.
    self.hasOpening = true
end

function card:Contest(ctx)
    local attack = self.hasOpening and 5 or 4
    self.hasOpening = false
    Attack(ctx, "Tu", "SheJi", attack)
end

return card
