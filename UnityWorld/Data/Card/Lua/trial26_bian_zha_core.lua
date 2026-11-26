-- 缝甲劲。每轮给一件法宝供 1 灵。己方受普通伤势时，下一次打击拼点 +2。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnWoundGained(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if HasKeyword(ctx, ctx.SourceCard, "Temp") then return end
    if not CheckWoundEvent(ctx, self.m_Owner, "Ally", 1) then return end
    AddNpcBuff(ctx, "Self", "Da_Changed_Once", 2, -1)
end

return card
