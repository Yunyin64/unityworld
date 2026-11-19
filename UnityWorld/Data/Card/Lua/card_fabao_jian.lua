-- 长剑法宝：主人用斩击赢得对拼后，下次招式对拼点数 +1。
local card = setmetatable({}, { __index = CardBase })

function card:OnContestOverflow(ctx)
    if ctx.Caster ~= self.m_Owner or ctx:GetStringValue("WinnerTypeName", "") ~= "Zhan" then return end
    -- 同一实例连续获胜时刷新一份加成，续期时保留。
    self.nextFormBonus = true
end

function card:OnBeforePairedContest(ctx)
    if not self.nextFormBonus then return end
    local contest = ctx:GetObject("ContestData")
    contest.ContestValue = contest.ContestValue + 1
    self.nextFormBonus = false
end

function card:OnPassiveDeactivated(ctx)
    self.nextFormBonus = false
end

return card
