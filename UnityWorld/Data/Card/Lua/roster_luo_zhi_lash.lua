local card = setmetatable({}, { __index = CardBase })

function card:OnManaUsed(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local round = self.m_Owner.Backpack.Round
    if self.rewardedRound == round or not CheckManaUsed(ctx, "Kinds", 2) then return end
    self.rewardedRound = round
    local mods = self.m_Owner:GetAllModifiers()
    for i = 0, mods.Count - 1 do
        if mods[i].DefineId == "Armor" then return end
    end
    AddArmor(ctx, "Self", 1)
end

return card
