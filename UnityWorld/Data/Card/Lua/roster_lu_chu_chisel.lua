local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.ShieldValue > 0 then
        Attack(ctx, "Jin", "Da", 3)
    else
        Attack(ctx, "Jin", "SheJi", 4)
    end
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 2)
end

return card
