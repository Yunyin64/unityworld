-- 灵活只决定何时请求离场；场地完成这次请求后才通知其他卡。
local Flexible = setmetatable({}, { __index = KeywordBase })

local function leaveIfEmpty(card, ctx)
    if (card.IsAmount and not card.HasAmount) or (card.HasConsume and card.ConsumeStack == 0) then
        RemoveCard(ctx, "Self", "Flexible")
    end
end

function Flexible.OnApply(card, ctx)
    if ctx.SourceCard == card then leaveIfEmpty(card, ctx) end
end

-- 法宝最后一发维持到真正失活，取消待发则没有 Apply。
Flexible.OnPassiveDeactivated = leaveIfEmpty
Flexible.OnPendingCancelled = leaveIfEmpty

-- 战斗在两次扫描之间结束时，最后一发维持的激活期也随之结束。
function Flexible.OnCombatEnd(card, ctx)
    if card:CheckPhase(CombatCardPhase.Active) then leaveIfEmpty(card, ctx) end
end

-- 脚本也可以在发动之外直接扣光资源，此时没有待发效果需要等待。
function Flexible.OnExhausted(card, ctx)
    if ctx.SourceCard == card and (card:CheckPhase(CombatCardPhase.Waiting)
        or card:CheckPhase(CombatCardPhase.Fulling)) then
        leaveIfEmpty(card, ctx)
    end
end
Flexible.OnAmmoExhausted = Flexible.OnExhausted

function Flexible.OnLeave(card, ctx)
    if ctx:GetStringValue("RemovalReason") == "Flexible" then
        ctx.Scene:TriggerCombatEvent("OnFlexible", ctx)
    end
end

LuaMgr:RegisterKeyword("Flexible", Flexible)
