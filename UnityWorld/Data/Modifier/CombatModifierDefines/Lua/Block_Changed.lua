-- 格挡强化
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyContest(ctx)
    local contest = ctx:GetObject("ContestData")
    if contest.ContestType ~= ContestType.Block then return end

    contest.ContestValue = contest.ContestValue + self.m_Self.CurrentStack
end

return buff
