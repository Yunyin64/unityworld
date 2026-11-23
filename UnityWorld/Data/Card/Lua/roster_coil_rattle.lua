local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, attack = self:GetEquip()
    -- 父壶的两色等量费用返回 Mix，这个招式明确为木。
    Attack(ctx, "Mu", "Da", attack)
end

return card
