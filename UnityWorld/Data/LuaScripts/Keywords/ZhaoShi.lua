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
    ZhaoShi.Advance(card.Owner)
end

-- 招式轮转：切换到下一张招式卡
function ZhaoShi.Advance(owner)
    local list = owner:GetZhaoShiList()
    local count = list.Count
    if count == 0 then
        owner:SetCurrentZhaoShiCardId(-1)
        return
    end

    local currentId = owner:GetCurrentZhaoShiCardId()
    local nextIdx = 0
    for i = 0, count - 1 do
        if list[i].Id == currentId then
            nextIdx = (i + 1) % count
            break
        end
    end

    local nextCard = list[nextIdx]
    owner:SetCurrentZhaoShiCardId(nextCard.Id)
    nextCard:ResetCD()
    owner:Log("[招式]  切换为【" .. nextCard.DisplayName .. "】")
end

LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
