-- 每次成功主动发动使用一份物品。实际扣份及变化事件由卡实例统一提交。
local Consume = setmetatable({}, { __index = KeywordBase })

Consume.ScanCost = {}

function Consume.ScanCost.CanPay(card)
    return card.HasConsume and card.ConsumeStack > 0
end

function Consume.ScanCost.Pay(card)
    card:TryConsume()
end

LuaMgr:RegisterKeyword("Consume", Consume)
