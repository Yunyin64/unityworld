local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Mu", 200, 0)
    ChangeAffinity(ctx, "Self", "Huo", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not CheckManaUsed(ctx, "Kinds", 2) then return end
    self.used = true
    RecoverMP(ctx, "Self", 3)
end

return card
