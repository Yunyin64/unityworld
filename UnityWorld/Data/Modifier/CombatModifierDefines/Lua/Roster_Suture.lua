local buff = setmetatable({}, { __index = BuffBase })

function buff:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    -- 本次基础补灵只结算一次。先移除状态，避免后续连锁重新消费旧实例。
    self.m_Owner:RemoveModifier(self.m_Self.DefineId)
    SelfDamage(ctx, "Self", 1)
end

return buff
