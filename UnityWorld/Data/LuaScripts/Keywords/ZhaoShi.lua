-- ZhaoShi Keyword
-- 招式卡：会拼点完成后，才继续走 CD 循环
local ZhaoShi = {}

--function ZhaoShi.PreStart(card, ctx)
--end

--function ZhaoShi.Start(card, ctx)
--end

function ZhaoShi.Tick(card, ctx)
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

--function ZhaoShi.Contest(card, ctx)
--end

function ZhaoShi.Apply(card, ctx)
    card:SetReady(false)
    card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
