-- ══════════════════════════════════════════════════════════════
-- JinDmgPercent - 金系增伤
-- 每层：金系拼点 +10%
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.Element == ElementType.Jin then
        contestData.ContestValue = contestData.ContestValue * (1 + 0.1 * self.CurrentStack)
    end
end

return mod
