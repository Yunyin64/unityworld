-- 常驻被动入场生效。开场和持续效果由卡脚本独立配置。
local Passive = setmetatable({ Main = true }, { __index = KeywordBase })
LuaMgr:RegisterKeyword("Passive", Passive)
