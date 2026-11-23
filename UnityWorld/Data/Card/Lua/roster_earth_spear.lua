local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    -- 开轮时枪可能失活，以实际赢拼时的己方轮号限次。
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    self.rewardedRound = round
    if self.m_Owner.ShieldValue < 3 then GainShield(ctx, "Self", 1) end
end

return card
