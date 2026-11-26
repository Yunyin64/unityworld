-- 木突刺。发起4点木系刺击拼点。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Mu", "Ci", 4)
end

return card
