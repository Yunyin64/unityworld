-- 霜回引。回复 3 MP，并尝试剥离对手一张随机卡上的 1 枚蓄灵。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    RecoverMP(ctx, "Self", 3)
    StripCardMana(ctx, "TargetRandom", "None", 1)
end

return card
