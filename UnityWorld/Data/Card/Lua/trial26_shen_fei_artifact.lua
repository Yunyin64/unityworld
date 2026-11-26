-- 悬冰鉴。激活期间，己方每次成功发起刺类法术，随机冻结对手一张可冻结的卡。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    if not HasKeyword(ctx, ctx.SourceCard, "Ci") then return end
    Freeze(ctx, "TargetRandom", 0)
end

return card
