-- 单次水系抗性
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyDamageIn(ctx)
    local hit = ctx:GetObject("DamageInfo")
    if hit.ElementType.Kind ~= BaseElementType.Shui then return end
    hit.Damage = hit.Damage + self.m_Self.CurrentStack
    self.m_Self:Consume()
end

return buff
