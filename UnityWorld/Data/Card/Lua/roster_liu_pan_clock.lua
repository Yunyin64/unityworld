local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Shui", 200, 0)
    ChangeAffinity(ctx, "Self", "Tu", 90, 0)
end

local function canUseCurrentMana(owner)
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
    local round = backpack.Round
    if round == 4 or round == 8 then
        Convert(ctx, "Self", "None", 2)
    elseif round > 8 and owner:GetMp() > 0 and backpack:EmptyCells().Count == 0 and not canUseCurrentMana(owner) then
        -- 舍去一枚用不掉的灵元，留出一格按原规则付 MP 补灵。
        ReduceMana(ctx, "Self", "None", 1)
    end
end

return card
