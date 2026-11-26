-- 点锋功。每轮给一件法宝供 1 灵。己方每次充满一张招式，扫描进度加 2 Tick。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not CheckManaCharged(ctx, "Any", "ZhaoShi", true) then return end
    Charge(ctx, "Self", 2)
end

return card
