local buff = setmetatable({}, { __index = BuffBase })

function buff:ModifyPairedContest(ctx)
    local opponent = ctx:GetObject("OpponentContestData")
    if not opponent.IsAttackType or opponent.OwnerNpc.Team == self.m_Owner.Team then return end

    opponent.ContestValue = math.max(0, opponent.ContestValue - self.m_Self.CurrentStack)
    self.m_Owner:RemoveModifier(self.m_Self.DefineId)
end

return buff
