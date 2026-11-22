local ContestImprovement = {}

local function create(matches, once)
    -- require 缓存工厂，状态表仍按 Buff 实例各自创建。
    local buff = setmetatable({}, { __index = BuffBase })

    function buff:ModifyContest(ctx)
        local contest = ctx:GetObject("ContestData")
        if not matches(contest) then return end

        contest.ContestValue = contest.ContestValue + self.m_Self.CurrentStack
        if once then
            self.m_Owner:RemoveModifier(self.m_Self.DefineId)
        end
    end

    return buff
end

function ContestImprovement.ForType(contestType, once)
    return create(function(contest) return contest.ContestType == contestType end, once)
end

function ContestImprovement.ForElement(element, once)
    return create(function(contest) return contest.IsAttackType and contest.Element.Kind == element end, once)
end

function ContestImprovement.ForAttack(once)
    return create(function(contest) return contest.IsAttackType end, once)
end

function ContestImprovement.ForDefense(once)
    return create(function(contest) return contest.IsDefenseType end, once)
end

return ContestImprovement
