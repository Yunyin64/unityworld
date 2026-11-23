-- 治疗提高
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyHealOut(ctx)
    ctx:Set("Amount", ctx:GetObject("Amount") + self.m_Self.CurrentStack)
end

return buff
