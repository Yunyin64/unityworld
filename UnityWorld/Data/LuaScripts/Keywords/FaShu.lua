-- FaShu Keyword
-- 法术卡：标准循环（消耗灵元 → CD → Contest → Apply）
local FaShu = {}

function FaShu.OnPreStart(card, ctx)
end

function FaShu.OnStart(card, ctx)
end

function FaShu.OnTick(card, ctx)
    if card:GetPhase() == CombatCardPhase.Waiting then
        card:CheckMana()
    end
end

function FaShu.Contest(card, ctx)
end

function FaShu.Apply(card, ctx)
end

LuaMgr:RegisterKeyword("FaShu", FaShu)
