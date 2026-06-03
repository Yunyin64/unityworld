-- FaBao Keyword
-- 法宝卡：触发式卡
-- 各法宝卡在自己的 OnXXX hook 中调用 CardBase.UseFabao(card, ctx) 触发效果
local FaBao = {}

-- 法宝的 Apply 结束后设为 Finished，防止意外进入 CD 流程

--function FaBao.PreStart(card, ctx)
--end

--function FaBao.Start(card, ctx)
--end

function FaBao.Tick(card, ctx)
    if card:CheckPhase(CombatCardPhase.Waiting)  then
        card:SetPhase(CombatCardPhase.InCD) 
    end
end

function FaBao.Contest(card, ctx)
    card:SetReady(false)
    card:SetPhase(CombatCardPhase.Finished)
end

--function FaBao.Apply(card, ctx)
--end

--- 全局法宝触发入口：检查灵元 → 成功则 Apply
function CardBase:UseFabao()
    local card = self.m_Self
    if card:CheckPhase(CombatCardPhase.CDFull)  then
        if card:TryPayMana() then
            card:ResetCD()
            card:SetReady(true)
        else
            Log("Use Fabao but <mana> not enough")
        end
    else
        card:LogCDInfo()
        Log("Use Fabao but <CD> not full")
    end
    if card:CheckPhase(CombatCardPhase.Passive)  then
        if card:TryPayMana() then
            card:SetReady(true)
        else
            Log("Use Fabao but <mana> not enough")
        end
    end
end

function CardBase:GetEquip()
    local card = self.m_Self
    local eq = card:GetEquipData()
    local name = eq:GetStringValue("DisplayName", "")
    local atk = eq:GetValue("Attack", 0)
    local def = eq:GetValue("Defend", 0)
    local spd = eq:GetValue("Speed", 0)
    local amo = eq:GetValue("Amount", 0)
    local elem = eq:GetStringValue("Element", "Wu")

    return name,atk,def,spd,amo,elem
end

LuaMgr:RegisterKeyword("FaBao", FaBao)
