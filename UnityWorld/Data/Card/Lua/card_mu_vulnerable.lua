-- 易伤。单次入伤修正用正层增加敌方承受的伤害。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddNpcBuff(ctx, "Target", "All_DamageIn_Changed_Once", 1, -1)
end

return card
