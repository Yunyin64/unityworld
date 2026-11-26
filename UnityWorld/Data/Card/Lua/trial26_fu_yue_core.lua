-- 归壁法。每轮给法宝供 1 枚土灵。己方每次用格或挡赢过攻击，尝试从对手一张随机卡上剥离 1 枚已蓄灵元。
local card = setmetatable({}, { __index = CardBase })

function card:Apply(ctx)
    AddArtifactPower(ctx, "Self", "Tu", 1, 0)
end

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    local kind = ctx:GetStringValue("WinnerTypeName", "")
    if kind ~= "Block" and kind ~= "Shield" then return end
    local rival = ctx:GetStringValue("LoserTypeName", "")
    if rival ~= "Zhan" and rival ~= "Ci" and rival ~= "Da" and rival ~= "SheJi" then return end
    StripCardMana(ctx, "TargetRandom", "None", 1)
end

return card
