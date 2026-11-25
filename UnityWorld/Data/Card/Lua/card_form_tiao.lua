local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local equipment = self.m_Self:GetEquipData()
    local attack = equipment:GetObject("Attack") or 0
    Attack(ctx, equipment:GetStringValue("Element", "None"), "Ci", attack)
end

function card:ModifyPairedContest(ctx)
    local opponent = ctx:GetObject("OpponentContestData")
    if opponent.ContestType ~= ContestType.Da then return end

    local equipment = self.m_Self:GetEquipData()
    local speed = math.floor(equipment:GetObject("Speed") or 0)
    local contest = ctx:GetObject("ContestData")
    contest.ContestValue = contest.ContestValue + speed
end

return card
