-- GongFa Keyword
-- 功法卡：（通常配合 Passive，提供被动效果/修正）
local GongFa = {}

function GongFa.PreStart(card, ctx)
end

function GongFa.Start(card, ctx)
end

--function GongFa.Tick(card, ctx)
--end

function GongFa.Apply(card, ctx)
    --card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("GongFa", GongFa)
