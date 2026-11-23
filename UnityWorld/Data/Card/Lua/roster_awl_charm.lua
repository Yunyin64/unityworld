local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or ctx:GetStringValue("WinnerTypeName", "") ~= "Ci" then return end
    if not CheckContestOutcome(ctx, self.m_Owner, "Win") then return end
    self.rewardedRound = round
    local mods = self.m_Owner:GetAllModifiers()
    for i = 0, mods.Count - 1 do
        if mods[i].DefineId == "Ci_Changed_Once" then return end
    end
    AddNpcBuff(ctx, "Self", "Ci_Changed_Once", 1, -1)
end

return card
