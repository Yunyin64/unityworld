local card = setmetatable({}, { __index = CardBase })

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.elements = {}
    self.count = 0
end

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner or not self.elements or self.count >= 3 then return end
    local element = ctx:GetStringValue("ChargedElement", "")
    if element == "" or self.elements[element] then return end
    self.elements[element] = true
    self.count = self.count + 1
end

function card:Contest(ctx)
    local attack = 3
    if self.count == 3 then attack = 4 end
    Attack(ctx, "Shui", "Ci", attack)
end

return card
