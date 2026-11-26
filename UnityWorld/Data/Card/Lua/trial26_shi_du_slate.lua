-- 碎瓦绳。己方灵盾每次被伤害打空，获得 1 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:OnShieldExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckShieldExhaustion(ctx, "Damage") then return end
    AddArmor(ctx, "Self", 1)
end

return card
