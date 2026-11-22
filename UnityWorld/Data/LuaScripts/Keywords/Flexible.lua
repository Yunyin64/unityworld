-- 消耗卡归零后，在最后一次效果结算完成时离场。
local Flexible = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("Flexible", Flexible)
