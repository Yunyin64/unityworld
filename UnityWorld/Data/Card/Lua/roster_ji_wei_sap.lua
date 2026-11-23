local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Mu", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local moved = ctx:GetObject("MovedCard")
    if moved == nil then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round then return end
    self.rewardedRound = round
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    source.CardTargets:Add(moved)
    StripCardMana(source, "Selected", "None", 1)
end

return card
