-- 单次火系强化
local buff = setmetatable({ SignedStacks = true }, { __index = BuffBase })

function buff:ModifyContest(ctx)
    local contest = ctx:GetObject("ContestData")
    if not contest.IsAttackType or contest.Element.Kind ~= BaseElementType.Huo then return end

    contest.ContestValue = contest.ContestValue + self.m_Self.CurrentStack
    self.m_Self:Consume()
end

return buff
