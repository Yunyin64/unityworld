-- 单次土系伤害提高
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyDamageOut(ctx)
    local hit = ctx:GetObject("DamageInfo")
    if hit.ElementType.Kind ~= BaseElementType.Tu then return end
    hit.Damage = hit.Damage + self.m_Self.CurrentStack
    self.m_Self:Consume()
end

return buff
