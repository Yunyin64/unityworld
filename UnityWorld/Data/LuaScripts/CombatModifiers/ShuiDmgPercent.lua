-- ══════════════════════════════════════════════════════════════
-- ShuiDmgPercent - 水系增伤
-- 每层：水系拼点 +10%
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.Element == ElementType.Shui then
        contestData.ContestValue = contestData.ContestValue * (1 + 0.1 * self.CurrentStack)
    end
end

return mod
