-- Passive 是可组合能力；不再由 Cooldown 派生，也不覆盖混合卡主动状态。
local Passive = setmetatable({}, { __index = KeywordBase })
function Passive.PreStart(card, ctx)
    if not card.BaseData.HasActiveAbility then
        card:SetPhase(CombatCardPhase.Passive)
    end
end
function Passive.Start(card, ctx)
    -- 仅纯被动保留原来的开场 Apply；混合卡必须取得合法主动机会。
    if not card.BaseData.HasActiveAbility then card:Apply() end
end
LuaMgr:RegisterKeyword("Passive", Passive)
