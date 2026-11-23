local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Jin", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnShieldExhausted(ctx)
    if ctx.Caster == nil or ctx.Caster.Team == self.m_Owner.Team then return end
    if ctx:GetObject("SourceNpc") ~= self.m_Owner then return end
    if not CheckShieldExhaustion(ctx, "Effect") or not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round then return end
    self.rewardedRound = round
    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Attack_Changed_Once" then return end
    end
    local source = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
    AddNpcBuff(source, "Self", "Attack_Changed_Once", 1, -1)
end

return card
