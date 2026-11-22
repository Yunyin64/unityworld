-- Consume Keyword
-- 消耗为普通关键词，次数由卡牌定义和当前实例提供。
local Consume = setmetatable({}, { __index = KeywordBase })

function Consume.CheckCondition(card)
    return card:GetStackMax() > 0
end

LuaMgr:RegisterKeyword("Consume", Consume)
