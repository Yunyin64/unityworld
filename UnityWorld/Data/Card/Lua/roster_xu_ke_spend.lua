local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Huo", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if self.used or not HasKeyword(ctx, ctx.SourceCard, "Consume") then return end
    self.used = true
    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Attack_Changed_Once" then return end
    end
    AddNpcBuff(ctx, "Self", "Attack_Changed_Once", 1, -1)
end

return card
