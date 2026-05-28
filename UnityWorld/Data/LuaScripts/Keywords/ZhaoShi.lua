-- ZhaoShi Keyword
-- 招式卡：（待定义，和法术可能有差异）
local ZhaoShi = {}

function ZhaoShi.OnPreStart(card, ctx)
end

function ZhaoShi.OnStart(card, ctx)
end

function ZhaoShi.OnTick(card, ctx)
    if card:GetPhase() == CombatCardPhase.Waiting then
        card:CheckMana()
    end
end

function ZhaoShi.Contest(card, ctx)
end

function ZhaoShi.Apply(card, ctx)
end

LuaMgr:RegisterKeyword("ZhaoShi", ZhaoShi)
