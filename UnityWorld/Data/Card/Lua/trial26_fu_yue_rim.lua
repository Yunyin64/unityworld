-- 转垛扣。己方每次实付至少 1 枚土灵元，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckManaUsed(ctx, "Tu", 1) then return end
    GainShield(ctx, "Self", 1)
end

return card
