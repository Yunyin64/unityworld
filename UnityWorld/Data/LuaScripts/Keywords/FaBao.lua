-- FaBao Keyword
-- 法宝卡：触发式被动卡，不走 CD 循环
-- 各法宝卡在自己的 OnXXX hook 中调用 CardBase.UseFabao(card, ctx) 触发效果
local FaBao = {}

-- 法宝的 Apply 结束后设为 Finished，防止意外进入 CD 流程

--function FaBao.PreStart(card, ctx)
--end

--function FaBao.Start(card, ctx)
--end

--function FaBao.Tick(card, ctx)
--end

--function FaBao.Contest(card, ctx)
--end

function FaBao.Apply(card, ctx)
    card:SetPhase(CombatCardPhase.Finished);
end

--- 全局法宝触发入口：检查灵元 → 成功则 Apply
--- 用法：CardBase.UseFabao(card, ctx)
function CardBase.UseFabao(card, ctx)
    if card:TryPayMana() then
        card:Apply()
    end
end

LuaMgr:RegisterKeyword("FaBao", FaBao)
