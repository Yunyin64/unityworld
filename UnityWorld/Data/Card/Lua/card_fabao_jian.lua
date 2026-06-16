-- 长剑法宝：当主人抽取灵元时，造成<武器>点<武器>系射击伤害
local card = setmetatable({}, { __index = CardBase })
card.CardData = { Size = 2, Cooldown = 0, CardType = "FaBao", ManaCost = { Jin = 1 } }
card.Keywords = {}

function card:OnBaseManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self:UseFabao()
end

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "SheJi", atk)
end


--function card:Apply(ctx)
--end

return card
