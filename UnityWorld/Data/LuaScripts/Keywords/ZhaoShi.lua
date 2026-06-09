-- ZhaoShi Keyword
-- 招式卡：串行轮转，同一时间只有当前卡走CD，Apply后advance到下一张
local ZhaoShi = {}

function ZhaoShi.Tick(card, ctx)
    local owner = card.Owner
    if owner:GetCurrentZhaoShiCardId() ~= card.Id then
        return
    end

    if card:CheckPhase(CombatCardPhase.Waiting) then
        card:SetPhase(CombatCardPhase.InCD)
    end
    if card:CheckPhase(CombatCardPhase.CDFull) then
        card:ResetCD()
        card:SetReady(true)
    end
end

function ZhaoShi.Contest(card, ctx)
    card:SetReady(false)
end

function ZhaoShi.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished)
    card.Owner:AdvanceZhaoShi()
end

LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
