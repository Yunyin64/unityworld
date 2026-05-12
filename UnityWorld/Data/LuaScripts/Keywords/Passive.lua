-- Passive Keyword
-- 被动卡：在 PreStart 阶段将卡牌 Phase 设为 Passive，跳过 CD 循环
local Passive = {}

function Passive.OnPreStart(card, ctx)
    card:SetPhase("Passive")
end



function Passive.OnStart(card, ctx)
    card:OnApply()
end

LuaMgr:RegisterKeyword("Passive", Passive)
