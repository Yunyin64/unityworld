-- Passive Keyword
-- 被动卡：在 PreStart 阶段将卡牌 Phase 设为 Passive，跳过 CD 循环
local Passive = {}

function Passive.PreStart(card, ctx)
    card:SetPhase(CombatCardPhase.Passive)
end



function Passive.Start(card, ctx)
    card:Apply()
end

LuaMgr:RegisterKeyword("Passive", Passive)
