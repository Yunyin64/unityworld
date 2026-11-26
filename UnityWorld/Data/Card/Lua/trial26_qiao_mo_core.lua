-- 刻脉法。己方弹药每次耗尽，回复 2 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnAmmoExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    RecoverMP(ctx, "Self", 2)
end

return card
