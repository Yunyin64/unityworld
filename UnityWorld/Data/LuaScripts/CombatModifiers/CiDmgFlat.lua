-- ══════════════════════════════════════════════════════════════
-- CiDmgFlat - 刺击强化
-- 每层：刺击拼点 +1
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.ContestType == ContestType.Ci then
        contestData.ContestValue = contestData.ContestValue + self.CurrentStack
    end
end

return mod
