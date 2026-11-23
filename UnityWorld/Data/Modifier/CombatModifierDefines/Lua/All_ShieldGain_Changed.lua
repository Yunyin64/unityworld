-- 灵盾获得量提高
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyShieldGain(ctx)
    ctx:Set("Amount", ctx:GetObject("Amount") + self.m_Self.CurrentStack)
end

return buff
