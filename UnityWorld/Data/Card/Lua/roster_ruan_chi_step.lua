local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "FaShu") and CheckManaUsed(ctx, "Huo", 2) then
        self.furnaceRound = self.m_Owner.Backpack.Round
    end
end

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    if self.furnaceRound == self.m_Owner.Backpack.Round then defend = defend + 1 end
    Defend(ctx, "Dodge", defend)
end

return card
