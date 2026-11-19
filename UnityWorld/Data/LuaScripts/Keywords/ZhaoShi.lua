-- 招式的父装备、本轮次数和末格资格由战斗核心检查。
local ZhaoShi = setmetatable({ Main = true }, { __index = KeywordBase })
LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
