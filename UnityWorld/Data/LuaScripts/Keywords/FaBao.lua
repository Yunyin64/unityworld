-- 法宝通过末格扫描开启或续期被动。卡脚本定义激活期内的实际效果。
-- 法宝的效果都必须是“激活时，每当xxxxTrigger时，（可能有condition）触发XXXaction”，不能是“当激活时就XXXX”
-- 法宝本质是一种，激活开关的被动触发器，不是一种以激活为开关的主动效果
local FaBao = {}

local function inEffect(phase)
    return phase == CombatCardPhase.Active or phase == CombatCardPhase.Applying
end

function FaBao.OnCardStart(card)
    if not card:CheckPhase(CombatCardPhase.InPending) then card:SetPhase(card.ChargePhase) end
end

function FaBao.CanCommitScan(card)
    return (card:CheckPhase(CombatCardPhase.Fulling) or card:CheckPhase(CombatCardPhase.Active))
        and KeywordBase.CanPayScanCosts(card)
end

function FaBao.OnScanCommitted(card)
    KeywordBase.PayScanCosts(card)
end

-- C# 在实付后通知成功，在解冻前通知失败。续期的方位取自本次发动的快照。
function FaBao.OnScanEnd(card, ctx, committed)
    if not committed then
        card:SetPhase(card.ChargePhase)
    elseif card:CheckPhase(CombatCardPhase.Active) then
        ctx.Scene:TriggerCombatEvent("OnArtifactRenewed", ctx)
    else
        card:SetPhase(CombatCardPhase.Active)
    end
end

-- 扫描失败、主动切换阶段和离场都会经过这里，先通知本卡，再通知全场。
function FaBao.OnPhaseChanged(card, ctx, previous)
    local active = inEffect(card.Phase)
    if inEffect(previous) == active then return end
    card:CallLua(active and "OnPassiveActivated" or "OnPassiveDeactivated")
    if ctx.Scene ~= nil and not ctx.Scene.IsFinished then
        ctx.Scene:TriggerCombatEvent(active and "OnArtifactActivated" or "OnArtifactDeactivated", ctx)
    end
end

function FaBao.CanRespond(card)
    return card.Owner ~= nil and card.Owner.IsActive and inEffect(card.Phase)
end

function FaBao.OnCardTick(card, ctx)
    if card.Owner ~= nil and card.Owner.IsActive and card:CheckPhase(CombatCardPhase.Active) then
        card:RunKeywordHooks("OnTick", ctx, "Tick")
    end
    if FaBao.CanRespond(card) then card:InvokeOwnHook("OnPassiveTick", ctx) end
end

LuaMgr:RegisterKeyword("FaBao", setmetatable({ Main = true, Lifecycle = FaBao }, { __index = KeywordBase }))
