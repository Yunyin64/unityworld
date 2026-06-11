-- ══════════════════════════════════════════════════════════════
--  刺击强化
-- 每层：刺击拼点 +1
-- ══════════════════════════════════════════════════════════════

local Buff = setmetatable({}, { __index = BuffBase })

function Buff:ModifyContest(ctx)
    local Data = ctx:GetObject("ContestData")
    if Data.ContestType == ContestType.Ci then
        Data.ContestValue = Data.ContestValue + self.m_Self.CurrentStack
    end
end

return Buff
