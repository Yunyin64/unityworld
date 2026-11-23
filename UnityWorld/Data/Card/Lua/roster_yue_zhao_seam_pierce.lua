local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster.Team == self.m_Owner.Team or ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    if not CheckShieldExhaustion(ctx, "Effect") then return end
    self.openRound = self.m_Owner.Backpack.Round
end

function card:Contest(ctx)
    local attack = 5
    if self.openRound ~= nil and self.m_Owner.Backpack.Round <= self.openRound + 1 then
        self.openRound = nil
        attack = attack + 1
    end
    Attack(ctx, "Shui", "Ci", attack)
end

return card
