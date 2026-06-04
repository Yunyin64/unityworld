-- 枪扫 - 基础枪招，造成<武器>点斩伤
local card = setmetatable({}, { __index = CardBase })
card.CardData = { Size = 1, Cooldown = 50, CardType = "ZhaoShi", ManaCost = {} }
card.Keywords = {}

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "Zhan", atk)
end

return card
