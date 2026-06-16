-- Shield Keyword
-- 盾牌类型通用机制
local Shield = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("Shield", Shield)
