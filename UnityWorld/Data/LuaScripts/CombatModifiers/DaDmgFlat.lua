-- ══════════════════════════════════════════════════════════════
-- DaDmgFlat - 钝击强化
-- 每层：钝击拼点 +1
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.ContestType == ContestType.Da then
        contestData.ContestValue = contestData.ContestValue + self.CurrentStack
    end
end

return mod
