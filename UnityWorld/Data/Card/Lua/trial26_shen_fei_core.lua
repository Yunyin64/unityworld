-- 窄窗诀。己方刺击每次赢得实际配对，获得 2 灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetStringValue("WinnerTypeName", "") ~= "Ci" then return end
    GainShield(ctx, "Self", 2)
end

return card
