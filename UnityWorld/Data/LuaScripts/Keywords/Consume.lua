-- Consume Keyword
-- 可消耗类型：StackMax > 0 时自动激活
local Consume = setmetatable({}, { __index = KeywordBase })

function Consume.CheckCondition(card)
    return card:GetStackMax() > 0
end

LuaMgr:RegisterKeyword("Consume", Consume)
