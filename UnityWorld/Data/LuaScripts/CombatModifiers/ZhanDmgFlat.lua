-- ══════════════════════════════════════════════════════════════
-- ZhanDmgFlat - 斩击强化
-- 每层：斩击拼点 +1
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.ContestType == ContestType.Zhan then
        contestData.ContestValue = contestData.ContestValue + self.CurrentStack
    end
end

return mod
