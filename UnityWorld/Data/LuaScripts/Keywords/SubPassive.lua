-- 非miankeyword为Passive的card，使用的附带被动效果的标志
-- 标记另有独立触发被动的卡牌，具体效果由卡牌脚本定义。
-- 被动的效果是，独立于时间扫描轴的，也就是说，是常态开启的。不要在一些法宝上随意的加这个keyword，因为法宝基本是激活时才有的触发。
-- 如果一张卡带有了，所有时间都可以生效的效果，才能加这个SubPassive
local SubPassive = setmetatable({}, { __index = KeywordBase })

LuaMgr:RegisterKeyword("SubPassive", SubPassive)
