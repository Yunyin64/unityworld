local card = setmetatable({}, { __index = CardBase })

function card:OnConsumed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Consume") then return end
    if self.m_Owner.ShieldValue < 2 then
        GainShield(ctx, "Self", 2)
    else
        Charge(ctx, "Self", 4)
    end
end

return card
