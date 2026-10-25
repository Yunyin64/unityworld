local card = {}
function card:Apply(ctx)
    self.m_Owner:ChangeShield(2)
end
return card
