-- Shoes Keyword
-- 鞋子类型通用机制
local Shoes = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("Shoes", Shoes)
