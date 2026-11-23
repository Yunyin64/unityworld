local card = setmetatable({}, { __index = CardBase })

function card:OnStraight(ctx)
    if ctx.Caster ~= self.m_Owner or not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not ctx.SourceCard:GetTags():Contains("攻击") then return end
    self.strikeRound = self.m_Owner.Backpack.Round
end

function card:Apply(ctx)
    local owner = self.m_Owner
    local shield = owner.ShieldValue
    if shield >= 3 then return end
    local amount = 2
    if shield == 0 and self.strikeRound ~= nil and owner.Backpack.Round <= self.strikeRound + 1 then
        amount = 3
    end
    GainShield(ctx, "Self", amount)
end

return card
