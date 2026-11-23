local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    GrantFreezeImmunityAura(ctx, "Down", "FaShu")
end

return card
