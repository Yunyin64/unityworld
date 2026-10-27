-- FaBao 声明事件条件机会；事件条件由卡/Keyword hook 判断。
local FaBao = setmetatable({}, { __index = KeywordBase })

-- ctx 必须是收到的原始事件。返回机会是否已收集；同事件两通道收集完后按位置提交。
-- 不得重建 Caster/SourceCard，不缓存失败到下个事件；返回 true 不保证最后争得待发槽。
function CardBase:UseFabao(ctx)
    return self.m_Self:TryUseFromEvent(ctx)
end

function CardBase:GetEquip()
    local eq = self.m_Self:GetEquipData()
    local name = eq:GetStringValue("DisplayName", "")
    local atk = eq:GetValue("Attack", 0)
    local def = eq:GetValue("Defend", 0)
    local spd = eq:GetValue("Speed", 0)
    local amo = eq:GetValue("Amount", 0)
    local elem = eq:GetStringValue("Element", "Wu")
    return name,atk,def,spd,amo,elem
end
LuaMgr:RegisterKeyword("FaBao", FaBao)
