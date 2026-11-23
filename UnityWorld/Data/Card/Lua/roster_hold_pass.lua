local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Tu", 200, 0)
    ChangeAffinity(ctx, "Self", "Shui", 90, 0)
    AddShieldCharge(ctx, "Self", "None", 3)
end

function card:OnShieldChargeReady(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Defend_Changed_Once" then return end
    end
    AddNpcBuff(ctx, "Self", "Defend_Changed_Once", 1, -1)
end

return card
