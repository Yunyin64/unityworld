-- 回水穗。己方招式每次实际充入一枚水灵元，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckManaCharged(ctx, "Shui", "ZhaoShi", false) then return end
    RecoverMP(ctx, "Self", 1)
end

return card
