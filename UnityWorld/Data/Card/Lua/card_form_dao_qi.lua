-- 刀气 - 基础刀招，造成<武器>点射伤
local card = setmetatable({}, { __index = CardBase })
card.CardData = { Size = 1, Cooldown = 6.5, CardType = "ZhaoShi", ManaCost = {} }
card.Keywords = {}

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "SheJi", atk)
end

return card
