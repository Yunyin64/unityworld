-- 剑刺 - 基础剑招，造成<武器>点刺伤
local card = setmetatable({}, { __index = CardBase })
card.CardData = { Size = 0, Cooldown = 4.5, CardType = "ZhaoShi", ManaCost = {} }
card.Keywords = {}

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "Ci", atk)
end

return card
