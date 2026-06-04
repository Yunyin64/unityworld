-- 妖兽爪击
local card = setmetatable({}, { __index = CardBase })
card.CardData = { Size = 1, Cooldown = 40, CardType = "ZhaoShi", ManaCost = {} }
card.Keywords = {}

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "Ci", atk)
end

return card
