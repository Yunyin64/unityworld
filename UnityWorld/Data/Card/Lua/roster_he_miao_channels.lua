local card = setmetatable({}, { __index = CardBase })

function card:Start(ctx)
    ChangeAffinity(ctx, "Self", "Mu", 200, 0)
    ChangeAffinity(ctx, "Self", "Shui", 90, 0)
end

function card:OnRoundStart(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    self.elements = {}
    self.count = 0
end

function card:OnManaCharged(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    if not self.elements or self.count >= 3 then return end
    local element = ctx:GetStringValue("ChargedElement", "")
    if element == "" or self.elements[element] then return end
    self.elements[element] = true
    self.count = self.count + 1
    if self.count == 3 then Draw(ctx, "Self", 2) end
end

return card
