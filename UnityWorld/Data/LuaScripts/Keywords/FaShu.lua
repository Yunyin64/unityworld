-- FaShu Keyword
-- 法术卡：标准循环（消耗灵元 → CD → Contest → Apply）
local FaShu = {}

--function FaShu.PreStart(card, ctx)
--end

--function FaShu.Start(card, ctx)
--end

function FaShu.Tick(card, ctx)
    if card:CheckPhase(CombatCardPhase.Waiting)  then
        if card:TryPayMana() then 
            card:SetPhase(CombatCardPhase.InCD) 
        end
    end
    if card:CheckPhase(CombatCardPhase.CDFull) then
        card:ResetCD()
        card:SetReady(true)
    end
end

function FaShu.Contest(card, ctx)
    card:SetReady(false)
    card:SetPhase(CombatCardPhase.Finished)
end

--function FaShu.Apply(card, ctx)
--end

LuaMgr:RegisterKeyword("FaShu", FaShu)
