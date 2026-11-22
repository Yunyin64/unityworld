-- 常驻被动入场生效。开场和持续效果由卡脚本独立配置。
local Passive = {}

-- 开场效果经过 Applying 时，被动仍在生效。
local function inEffect(phase)
    return phase == CombatCardPhase.Active or phase == CombatCardPhase.Applying
end

function Passive.OnCardPreStart(card, ctx)
    card:CallLua("PreStart", ctx)
end

function Passive.OnCardStart(card, ctx)
    if not card:CheckPhase(CombatCardPhase.InPending) then card:SetPhase(CombatCardPhase.Active) end
    card:CallLua("Start", ctx)
    if card.LuaHooks:ContainsKey("Apply") then card:Apply() end
end

function Passive.OnCardTick(card, ctx)
    card:RunKeywordHooks("OnTick", ctx, "Tick")
    if Passive.CanRespond(card) then card:InvokeOwnHook("OnPassiveTick", ctx) end
end

function Passive.CanRespond(card)
    return card.Owner ~= nil and card.Owner.IsActive and inEffect(card.Phase)
end

function Passive.OnPhaseChanged(card, ctx, previous)
    local active = inEffect(card.Phase)
    if inEffect(previous) == active then return end
    card:CallLua(active and "OnPassiveActivated" or "OnPassiveDeactivated")
    return active
end

function Passive.ApplyEndPhase(card)
    return CombatCardPhase.Active
end

LuaMgr:RegisterKeyword("Passive", setmetatable({ Main = true, Lifecycle = Passive }, { __index = KeywordBase }))
