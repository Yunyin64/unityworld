-- 临刃功。每轮给一件法宝供 1 灵。功法左侧共边法术每次发动，回 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnUse(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not WasCardUsedOnSide(ctx, self.m_Self, "Left", "FaShu") then return end
    RecoverMP(ctx, "Self", 1)
end

return card
