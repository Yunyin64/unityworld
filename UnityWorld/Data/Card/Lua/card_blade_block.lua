-- 刀挡 - 格挡<武器>点
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local name, atk, def, spd, amo, elem = self:GetEquip()
    Defend(ctx, "Block", def)
end

return card
