-- ══════════════════════════════════════════════════════════════
-- AllDmgFlat - 全攻击强化
-- 每层：所有攻击类拼点 +1
-- ══════════════════════════════════════════════════════════════

local mod = {}

function mod:ModifyContest(npc, contestData)
    if contestData.IsAttackType then
        contestData.ContestValue = contestData.ContestValue + self.CurrentStack
    end
end

return mod
