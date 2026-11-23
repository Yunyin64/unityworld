-- 单次MP 回复量提高
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyMpRecovery(ctx)
    ctx:Set("Amount", ctx:GetObject("Amount") + self.m_Self.CurrentStack)
    self.m_Self:Consume()
end

return buff
