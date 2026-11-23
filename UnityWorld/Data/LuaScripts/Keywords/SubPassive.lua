-- 非miankeyword为Passive的card，使用的附带被动效果的标志
-- 标记另有独立触发被动的卡牌，具体效果由卡牌脚本定义。
local SubPassive = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("SubPassive", SubPassive)
