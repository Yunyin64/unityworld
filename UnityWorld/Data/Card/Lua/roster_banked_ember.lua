local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Huo", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not WasCardUsedOnSide(ctx, self.m_Self, "Down", "FaShu") then return end
    -- 同轮只保存同一笔。用己方扫描轮号避开敌方先开轮。
    self.shieldRound = self.m_Owner.Backpack.Round + 1
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.shieldRound ~= ctx:GetObject("Round") then return end
    self.shieldRound = nil
    if self.m_Owner.ShieldValue < 2 then GainShield(ctx, "Self", 2) end
end

return card
