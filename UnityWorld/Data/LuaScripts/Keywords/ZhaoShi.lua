-- ZhaoShi 保留背包扫描机会；旧串行 CD 轮转等待独立重设计。
local ZhaoShi = setmetatable({}, { __index = KeywordBase })
LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
