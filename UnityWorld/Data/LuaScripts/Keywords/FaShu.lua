-- FaShu 仅声明扫描机会；资格、资源和入槽由 C# 统一提交。
local FaShu = setmetatable({}, { __index = KeywordBase })
LuaMgr:RegisterKeyword("FaShu", FaShu)
