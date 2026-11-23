-- 钝击伤害提高
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyDamageOut(ctx)
    local hit = ctx:GetObject("DamageInfo")
    if hit.damageType ~= DamageType.Da then return end
    hit.Damage = hit.Damage + self.m_Self.CurrentStack
end

return buff
