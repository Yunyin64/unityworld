-- Wound Keyword
-- 伤势卡：（待定义行为，自伤/debuff/特殊触发）
local Wound = setmetatable({}, { __index = KeywordBase })

function Wound.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("Wound", Wound)
