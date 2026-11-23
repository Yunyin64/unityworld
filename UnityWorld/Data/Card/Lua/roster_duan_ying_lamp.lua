local card = setmetatable({}, { __index = CardBase })

function card:OnPassiveActivated(ctx)
    GrantFreezeImmunityAura(ctx, "Up", "FaShu")
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or not WasCardUsedOnSide(ctx, self.m_Self, "Up", "FaShu") then return end
    self.rewardedRound = round
    local mods = self.m_Owner:GetAllModifiers()
    for i = 0, mods.Count - 1 do
        if mods[i].DefineId == "Defend_Changed_Once" then return end
    end
    AddNpcBuff(ctx, "Self", "Defend_Changed_Once", 1, -1)
end

return card
