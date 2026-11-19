-- 法宝通过末格扫描开启或续期被动。卡脚本定义激活期内的实际效果。
local FaBao = setmetatable({ Main = true }, { __index = KeywordBase })
LuaMgr:RegisterKeyword("FaBao", FaBao)
