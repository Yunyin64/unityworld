local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local attack = 4
    local target = self.m_Owner:GetTarget()
    if target ~= nil and target.ShieldValue == 0 then
        attack = attack + 1
    end
    Attack(ctx, "Tu", "SheJi", attack)
end

function card:Apply(ctx)
    ArmorBreak(ctx, "Target", 1)
end

return card
