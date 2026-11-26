return function(elementName)
    local element = BaseElementType[elementName]
    local buff = setmetatable({}, { __index = BuffBase })

    function buff:ModifyRoundStart(ctx)
        -- 本次轮首的场上法宝沿用同一份快照。每充入一枚，再读卡上当前的缺口。
        local artifacts = ctx:GetObject("ArtifactPowerCards")
        if artifacts == nil then
            artifacts = {}
            local field = self.m_Owner:GetField()
            for i = 0, field.Count - 1 do
                local card = field[i]
                if card.BaseData.MainKeyword == CardMainKeyword.FaBao then
                    artifacts[#artifacts + 1] = card
                end
            end
            table.sort(artifacts, function(a, b) return a.Id < b.Id end)
            ctx:Set("ArtifactPowerCards", artifacts)
        end

        local scene = ctx.Scene
        local supplied = 0
        while supplied < self.m_Self.CurrentStack do
            local specific, generic = {}, {}
            for _, card in ipairs(artifacts) do
                local specificColors, genericColors = {}, {}
                local accepted = card:GetChargeableManaElements()
                for i = 0, accepted.Count - 1 do
                    local color = accepted[i]
                    if elementName == "None" or color.Kind == element then
                        local options = card:NeedsSpecificMana(color) and specificColors or genericColors
                        options[#options + 1] = color
                    end
                end
                if #specificColors > 0 then specific[#specific + 1] = { Card = card, Colors = specificColors } end
                if #genericColors > 0 then generic[#generic + 1] = { Card = card, Colors = genericColors } end
            end

            local candidates = #specific > 0 and specific or generic
            if #candidates == 0 then return end
            -- 先抽卡再抽颜色，一个缺多色的法宝仍只占一份选卡权重。
            local selected = candidates[scene:RandomIndex(#candidates) + 1]
            selected.Card:TryChargeMana(selected.Colors[scene:RandomIndex(#selected.Colors) + 1])
            supplied = supplied + 1
        end
    end

    return buff
end
