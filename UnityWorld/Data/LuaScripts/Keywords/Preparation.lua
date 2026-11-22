-- 消耗卡在正式战斗结算时补满次数。
local Preparation = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("Preparation", Preparation)
