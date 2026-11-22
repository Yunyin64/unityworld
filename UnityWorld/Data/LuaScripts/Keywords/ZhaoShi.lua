-- 招式绑定装备实例；同一招式本轮成功发动的次数受自身上限约束。
local ZhaoShi = {}

function ZhaoShi.OnCardPreStart(card, ctx)
    card:CallLua("PreStart", ctx)
end

function ZhaoShi.OnCardStart(card, ctx)
    if not card:CheckPhase(CombatCardPhase.InPending) then card:SetPhase(card.ChargePhase) end
    card:CallLua("Start", ctx)
end

function ZhaoShi.OnCardTick(card, ctx)
    card:InvokeOwnHook("Tick", ctx)
    card:RunKeywordHooks("OnTick", ctx, "Tick")
end

function ZhaoShi.CanCommitScan(card)
    return card:CheckPhase(CombatCardPhase.Fulling) and card.UseCount == card.ApplyCount
        and (not card.ProducesContest or card.Owner.PendingSlot == nil)
        and KeywordBase.CanPayScanCosts(card)
        and card.Owner:HasCombatFormSource(card)
        and card.FormUsesThisRound < card.BaseData.MaxUsesPerRound
end

function ZhaoShi.OnScanCommitted(card)
    KeywordBase.PayScanCosts(card)
    card:RecordFormUse()
    card:SetPhase(CombatCardPhase.Active)
end

function ZhaoShi.OnScanEnd(card, ctx, committed)
    if committed and card.ProducesContest then card:CallLua("Contest") end
end

-- 实际配对才领取场上卡牌提供的招式强化。入槽和轮末直击不会经过此处。
function ZhaoShi.OnBeforePairedContest(card, ctx, contest)
    card.Owner:ApplyFieldContestModifiers(contest)
end

function ZhaoShi.PaidUseAwaitsApply(card)
    return card.UseCount ~= card.ApplyCount
end

function ZhaoShi.ApplyEndPhase(card)
    if ZhaoShi.PaidUseAwaitsApply(card) then return CombatCardPhase.Finished end
end

function ZhaoShi.CanRespond(card, ctx, fieldEffect)
    return not fieldEffect
end

LuaMgr:RegisterKeyword("ZhaoShi", setmetatable({ Main = true, Lifecycle = ZhaoShi }, { __index = KeywordBase }))
