-- Armor Keyword
-- 护甲类型通用机制
local Armor = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("Armor", Armor)
