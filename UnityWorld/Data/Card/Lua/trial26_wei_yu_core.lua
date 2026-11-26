-- 引弦法。每轮给一件法宝供 1 灵。己方射击每次赢得实际配对，按拼点胜差破除对手等量灵盾。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "None", 1, 0)
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if ctx:GetStringValue("WinnerTypeName", "") ~= "SheJi" then return end
    ArmorBreak(ctx, "Target", ctx:GetObject("Overflow"))
end

return card
