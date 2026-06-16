-- Passive Keyword
-- 被动卡：Cooldown <= 0 时自动激活，跳过 CD 循环
local Passive = setmetatable({}, { __index = KeywordBase })

function Passive.CheckCondition(card)
    return card:GetCooldown() <= 0
end

function Passive.PreStart(card, ctx)
    card:SetPhase(CombatCardPhase.Passive)
end

function Passive.Start(card, ctx)
    card:Apply()
end

LuaMgr:RegisterKeyword("Passive", Passive)
