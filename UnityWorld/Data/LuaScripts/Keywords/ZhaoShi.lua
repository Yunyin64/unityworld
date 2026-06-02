-- ZhaoShi Keyword
-- 招式卡：会拼点完成后，才继续走 CD 循环
local ZhaoShi = {}

function ZhaoShi.PreStart(card, ctx)
end

function ZhaoShi.Start(card, ctx)
end

function ZhaoShi.Tick(card, ctx)
    if card:GetPhase() == CombatCardPhase.Waiting then
        card:CheckMana()
    end
end

--function ZhaoShi.Contest(card, ctx)
--end

function ZhaoShi.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
