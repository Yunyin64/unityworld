-- 换岸诀。己方每次实际移动一张对方的卡，回复 1 MP。
local card = setmetatable({}, { __index = CardBase })

function card:OnDisplaceOtherCard(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    RecoverMP(ctx, "Self", 1)
end

return card
