local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "FaShu") and CheckManaUsed(ctx, "Huo", 1) then
        self.spellRound = self.m_Owner.Backpack.Round
    end
end

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    if self.spellRound == self.m_Owner.Backpack.Round then attack = attack + 1 end
    Attack(ctx, "Huo", "SheJi", attack)
end

return card
