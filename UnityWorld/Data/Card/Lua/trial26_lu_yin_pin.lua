-- 束锋扣。己方招式每次赢得实际配对后，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "ZhaoShi") then return end
    if not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    GainShield(ctx, "Self", 1)
end

return card
