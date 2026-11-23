local card = setmetatable({}, { __index = CardBase })

local function hasUsableResources(npc)
    if npc:GetMp() > 0 or npc.Backpack.Mana.Count > 0 or npc.PendingSlot ~= nil then return true end
    local field = npc:GetField()
    for i = 0, field.Count - 1 do
        if field[i]:HasActivationResources() then return true end
    end
    return false
end

function card:OnBaseManaDraw(ctx)
    local owner = self.m_Owner
    if ctx.Caster ~= owner then return end
    if not self.cleared and owner:GetMp() <= 20 then
        self.cleared = true
        local affinity = owner:GetAffinity()
        ChangeAffinity(ctx, "Self", "Jin", -math.max(0, affinity.Jin), 0)
        ChangeAffinity(ctx, "Self", "Huo", -math.max(0, affinity.Huo), 0)
        ChangeAffinity(ctx, "Self", "Tu", -math.max(0, affinity.Tu), 0)
    end
    Convert(ctx, "Self", "None", 1)

    if self.reclaimed or hasUsableResources(owner) then return end
    local target = owner:GetTarget()
    if target == nil or not target.IsActive or hasUsableResources(target) then return end

    local field = owner:GetField()
    for i = 0, field.Count - 1 do
        local artifact = field[i]
        if artifact.LitCount > 0 and artifact:HasKeyword("FaBao") then
            -- 双方都用尽可动用的灵元时，只回收一枚锁在未蓄满法宝上的灵元。
            local relative = ctx:CreateContextFromCard(self.m_Self, ctx.Scene)
            relative.CardTargets:Add(artifact)
            StripCardMana(relative, "Selected", "None", 1)
            local removed = relative:GetObject("RemovedCount")
            if removed ~= nil and removed > 0 then
                self.reclaimed = true
                RecoverMP(relative, "Self", removed)
            end
            return
        end
    end
end

return card
