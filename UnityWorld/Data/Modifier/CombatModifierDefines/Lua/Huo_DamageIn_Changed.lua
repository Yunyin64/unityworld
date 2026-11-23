-- 火系抗性
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyDamageIn(ctx)
    local hit = ctx:GetObject("DamageInfo")
    if hit.ElementType.Kind ~= BaseElementType.Huo then return end
    hit.Damage = hit.Damage + self.m_Self.CurrentStack
end

return buff
