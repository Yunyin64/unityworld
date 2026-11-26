-- 缝铁甲。激活期间己方获得普通伤势时，获得 3 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnWoundGained(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "Temp") then return end
    if not CheckWoundEvent(ctx, self.m_Owner, "Ally", 1) then return end
    GainShield(ctx, "Self", 3)
end

return card
