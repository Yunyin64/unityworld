-- 侧刃刀。激活期间，下侧共边法术每次发动，都让装备本轮攻击 +2。
local card = setmetatable({}, { __index = CardBase })

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not WasCardUsedOnSide(ctx, self.m_Self, "Down", "FaShu") then return end
    -- 此时来源是下方辅助法术，先按方位重新选中上方的法宝。
    SelectDirectionalCards(ctx, "Up", "FaBao")
    ChangeEquipmentStat(ctx, "Selected", "Attack", 2, 1)
end

return card
