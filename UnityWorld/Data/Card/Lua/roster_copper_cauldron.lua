local card = setmetatable({}, { __index = CardBase })

function card:OnPassiveActivated(ctx)
    GrantFreezeImmunityAura(ctx, "Up", "FaShu")
end

return card
