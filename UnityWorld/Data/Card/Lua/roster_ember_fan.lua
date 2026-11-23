local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    -- 扇在轮开始时可能失活，直接以发动时的己方轮号限次。
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or not WasCardUsedOnSide(ctx, self.m_Self, "Neighbor", "FaShu") then return end
    self.rewardedRound = round
    Haste(ctx, "Self", 5)
end

return card
