-- 软枝贴。己方每次治愈普通伤势，获得 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnWoundHealed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "Temp") then return end
    GainShield(ctx, "Self", 2)
end

return card
