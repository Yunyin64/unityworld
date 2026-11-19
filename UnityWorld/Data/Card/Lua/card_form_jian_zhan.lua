-- 剑斩 - 基础剑招，造成<武器>点斩伤
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Attack(ctx, elem, "Zhan", atk)
end

return card
