-- 雁羽射。发起 10 点金射击拼点。每当己方弹药卡打空，获得 2 点一次性护甲。
local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    Attack(ctx, "Jin", "SheJi", 10)
end

function card:OnAmmoExhausted(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    AddArmor(ctx, "Self", 2)
end

return card
