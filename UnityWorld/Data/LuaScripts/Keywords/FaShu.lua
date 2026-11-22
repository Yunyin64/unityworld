-- 法术蓄满灵元后等待末格扫描；产生拼点时，效果延迟到直击或对拼后结算。
local FaShu = {}

function FaShu.OnCardPreStart(card, ctx)
    card:CallLua("PreStart", ctx)
end

function FaShu.OnCardStart(card, ctx)
    if not card:CheckPhase(CombatCardPhase.InPending) then card:SetPhase(card.ChargePhase) end
    card:CallLua("Start", ctx)
end

function FaShu.OnCardTick(card, ctx)
    card:InvokeOwnHook("Tick", ctx)
    card:RunKeywordHooks("OnTick", ctx, "Tick")
end

function FaShu.CanCommitScan(card)
    return card:CheckPhase(CombatCardPhase.Fulling) and card.UseCount == card.ApplyCount
        and (not card.ProducesContest or card.Owner.PendingSlot == nil)
        and KeywordBase.CanPayScanCosts(card)
end

function FaShu.OnScanCommitted(card)
    KeywordBase.PayScanCosts(card)
    card:SetPhase(CombatCardPhase.Active)
end

function FaShu.OnScanEnd(card, ctx, committed)
    if committed and card.ProducesContest then card:CallLua("Contest") end
end

function FaShu.PaidUseAwaitsApply(card)
    return card.UseCount ~= card.ApplyCount
end

function FaShu.ApplyEndPhase(card)
    if FaShu.PaidUseAwaitsApply(card) then return CombatCardPhase.Finished end
end

function FaShu.CanRespond(card, ctx, fieldEffect)
    return not fieldEffect
end

LuaMgr:RegisterKeyword("FaShu", setmetatable({ Main = true, Lifecycle = FaShu }, { __index = KeywordBase }))
