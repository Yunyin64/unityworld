-- 迸炭。给对手加 1 层火行减益。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddElementBuff(ctx, "Target", "Huo", true, 1)
end

return card
