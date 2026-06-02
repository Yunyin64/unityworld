-- Item Keyword
-- 物品卡
local Item = {}

--function Item.PreStart(card, ctx)
--end

--function Item.Start(card, ctx)
--end

--function Item.Tick(card, ctx)
--end

--function Item.Contest(card, ctx)
--end

function Item.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("Item", Item)
