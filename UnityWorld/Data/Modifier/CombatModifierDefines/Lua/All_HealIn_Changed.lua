-- 受疗降低
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyHealIn(ctx)
    ctx:Set("Amount", ctx:GetObject("Amount") + self.m_Self.CurrentStack)
end

return buff
