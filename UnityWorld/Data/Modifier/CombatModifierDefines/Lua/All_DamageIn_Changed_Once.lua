-- 单次全伤害抗性
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyDamageIn(ctx)
    local hit = ctx:GetObject("DamageInfo")
    hit.Damage = hit.Damage + self.m_Self.CurrentStack
    self.m_Self:Consume()
end

return buff
