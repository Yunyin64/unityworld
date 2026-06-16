function OnKeywordCheck(combatCard)
    local base = combatCard.BaseData
    if base.Cooldown <= 0 then
        combatCard:AddKeyword("Passive")
    end
    if base.AmountMax > 0 then
        combatCard:AddKeyword("Amount")
    end
    if base.StackMax > 0 then
        combatCard:AddKeyword("Consume")
    end
end
