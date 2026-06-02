-- FaShu Keyword
-- 法术卡：标准循环（消耗灵元 → CD → Contest → Apply）
local FaShu = {}

--function FaShu.PreStart(card, ctx)
--end

--function FaShu.Start(card, ctx)
--end

function FaShu.Tick(card, ctx)
    if card:GetPhase() == CombatCardPhase.Waiting then
        card:CheckMana()
    end
end

function FaShu.Contest(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

--function FaShu.Apply(card, ctx)
--end

LuaMgr:RegisterKeyword("FaShu", FaShu)
