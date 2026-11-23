local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Shui", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetStringValue("WinnerTypeName", "") ~= "Dodge" then return end
    if not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    local modifiers = self.m_Owner:GetAllModifiers()
    for i = 0, modifiers.Count - 1 do
        if modifiers[i].DefineId == "Da_Changed_Once" then return end
    end
    AddNpcBuff(ctx, "Self", "Da_Changed_Once", 2, -1)
end

return card
