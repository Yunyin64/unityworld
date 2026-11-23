local card = setmetatable({}, { __index = CardBase })

local function canUseCurrentMana(owner)
    if owner.PendingSlot ~= nil then return true end
    local field = owner:GetField()
    local elements = owner:GetUnassignedElements()
    for i = 0, field.Count - 1 do
        local fieldCard = field[i]
        if fieldCard:HasActivationResources() then return true end
        for j = 0, elements.Length - 1 do
            if fieldCard:CanAcceptMana(elements[j]) then return true end
        end
    end
    return false
end

function card:OnBaseManaDraw(ctx)
    local owner = self.m_Owner
    if ctx.Caster ~= owner then return end
    local backpack = owner.Backpack
    local mp = owner:GetMp()
    if backpack.Round >= 9 and mp > 0 and mp >= owner:GetCombatMpMax()
        and backpack:EmptyCells().Count == 0 and not canUseCurrentMana(owner) then
        -- 满 MP 时无法回收。舍去一枚闲置灵元，让本轮补灵照常支付 MP。
        ReduceMana(ctx, "Self", "None", 1)
    else
        Convert(ctx, "Self", "None", 1)
    end
end

return card
