local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "FaShu") and CheckManaUsed(ctx, "Huo", 2) then
        self.furnaceRound = self.m_Owner.Backpack.Round
    end
end

function card:Contest(ctx)
    local attack = 4
    if self.furnaceRound ~= nil and self.m_Owner.Backpack.Round <= self.furnaceRound + 1 then
        self.furnaceRound = nil
        attack = attack + 2
    end
    Attack(ctx, "Tu", "SheJi", attack)
end

return card
