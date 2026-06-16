-- Item Keyword
-- 物品卡
local Item = setmetatable({}, { __index = KeywordBase })

function Item.CheckCondition(card)
    return card.IsItemCard
end

function Item.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("Item", Item)
