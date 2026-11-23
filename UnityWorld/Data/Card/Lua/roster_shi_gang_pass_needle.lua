local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner or ctx:GetStringValue("WinnerTypeName", "") ~= "Block" then return end
    if not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    self.blockRound = self.m_Owner.Backpack.Round
end

function card:Contest(ctx)
    local attack = 5
    if self.blockRound ~= nil and self.m_Owner.Backpack.Round <= self.blockRound + 1 then
        self.blockRound = nil
        attack = attack + 1
    end
    Attack(ctx, "Shui", "Ci", attack)
end

return card
