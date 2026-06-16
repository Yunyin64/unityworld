-- GongFa Keyword
-- 功法卡：（通常配合 Passive，提供被动效果/修正）
local GongFa = setmetatable({}, { __index = KeywordBase })

function GongFa.PreStart(card, ctx)
end

function GongFa.Start(card, ctx)
end

function GongFa.Apply(card, ctx)
end

LuaMgr:RegisterKeyword("GongFa", GongFa)
