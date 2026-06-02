-- Wound Keyword
-- 伤势卡：（待定义行为，自伤/debuff/特殊触发）
local Wound = {}

--function Wound.PreStart(card, ctx)
--end

--function Wound.Start(card, ctx)
--end

--function Wound.Tick(card, ctx)
--end

--function Wound.Contest(card, ctx)
--end

function Wound.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("Wound", Wound)
