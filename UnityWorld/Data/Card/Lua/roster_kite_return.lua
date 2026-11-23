local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Mu", 200, 0)
    ChangeAffinity(ctx, "Self", "Shui", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.used = false
end

function card:OnCardDisplaced(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not HasKeyword(ctx, ctx.SourceCard, "FaShu") then return end
    self.used = true
    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Zhan_Changed_Once" then return end
    end
    AddNpcBuff(ctx, "Self", "Zhan_Changed_Once", 1, -1)
end

return card
