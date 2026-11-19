-- 刀打 - 基础刀招，造成<武器>点打伤
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "Da", atk)
end

return card
