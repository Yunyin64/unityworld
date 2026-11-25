-- 临时卡在实际离场或战末登记销毁。
local Temp = setmetatable({}, { __index = KeywordBase })

function Temp.OnLeave()
    return "DestroyCard"
end

Temp.OnCombatEnd = Temp.OnLeave
LuaMgr:RegisterKeyword("Temp", Temp)
