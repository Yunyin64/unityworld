-- ══════════════════════════════════════════════════════════════
-- MuDmgPercent - 木系增伤
-- 每层：木系拼点 +10%
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.Element == ElementType.Mu then
        contestData.ContestValue = contestData.ContestValue * (1 + 0.1 * self.CurrentStack)
    end
end

return mod
