local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Tu", 200, 0)
    ChangeAffinity(ctx, "Self", "Shui", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetObject("Round") % 3 ~= 1 or self.m_Owner.ShieldValue > 0 then return end
    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Deflection" then return end
    end
    AddDeflection(ctx, "Self", 2)
end

return card
