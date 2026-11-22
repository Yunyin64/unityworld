-- 弹药限制本次扫描发动。法宝用最后一发买下的整个激活期仍可响应卡面效果。
local Amount = setmetatable({}, { __index = KeywordBase })

Amount.ScanCost = {}

function Amount.ScanCost.CanPay(card)
    return card.HasAmount
end

function Amount.ScanCost.Pay(card)
    card:ConsumeAmount()
end

LuaMgr:RegisterKeyword("Amount", Amount)
