-- 射击抗性
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyDamageIn(ctx)
    local hit = ctx:GetObject("DamageInfo")
    if hit.damageType ~= DamageType.SheJi then return end
    hit.Damage = hit.Damage + self.m_Self.CurrentStack
end

return buff
