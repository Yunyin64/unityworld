-- Size / 形状 / 消耗由 JSON 定义；Lua 只描述效果。
local card = {}
function card:Contest(ctx)
    Attack(ctx, "Jin", "Zhan", 6)
end
return card
