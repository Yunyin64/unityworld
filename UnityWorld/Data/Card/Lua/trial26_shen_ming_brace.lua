-- 护盏箍。己方每件法宝失活时，获得 1 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnArtifactDeactivated(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    GainShield(ctx, "Self", 1)
end

return card
