local card = setmetatable({}, { __index = CardBase })

function card:Contest(ctx)
    local _, _, defend = self:GetEquip()
    local field = self.m_Owner:GetField()
    for i = 0, field.Count - 1 do
        local parent = field[i]
        if parent.Id == self.m_Self.ParentCardId and parent:CheckPhase(CombatCardPhase.Active) then
            defend = defend + 1
            break
        end
    end
    Defend(ctx, "Block", defend)
end

return card
