-- 涉石履。每次有己方卡实际滑动，获得 1 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:OnCardDisplaced(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    AddArmor(ctx, "Self", 1)
end

return card
