-- 优先冻结敌方范围内未冻结卡；下一次完整扫描跳过并解冻，不计时、不叠加。
local card = setmetatable({}, { __index = CardBase })
function card:Apply(ctx)
    Freeze(ctx, "TargetRandom", 0)
end
return card
