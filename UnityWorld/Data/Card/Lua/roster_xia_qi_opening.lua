local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Jin", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnStraight(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") or not ctx.SourceCard:GetTags():Contains("攻击") then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round then return end
    self.rewardedRound = round
    RecoverMP(ctx, "Self", 2)
end

return card
