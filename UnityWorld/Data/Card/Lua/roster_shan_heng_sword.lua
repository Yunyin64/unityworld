local card = setmetatable({}, { __index = CardBase })

local function isBoundForm(self, ctx, source)
    return source ~= nil and source.ParentCardId == self.m_Self.Id and HasKeyword(ctx, source, "ZhaoShi")
end

function card:OnContestOverflow(ctx)
    -- 平局的 Caster 不一定是剑主，从配对双方中选择本人的来源卡。
    local source
    if ctx:GetObject("ContestA") == self.m_Owner then
        source = ctx:GetObject("ContestCardA")
    elseif ctx:GetObject("ContestB") == self.m_Owner then
        source = ctx:GetObject("ContestCardB")
    end
    if isBoundForm(self, ctx, source) and not CheckContestOutcome(ctx, self.m_Owner, "Win") then
        self.ready = true
    end
end

function card:OnBeforePairedContest(ctx)
    if not self.ready or not isBoundForm(self, ctx, ctx.SourceCard) then return end
    local contest = ctx:GetObject("ContestData")
    contest.ContestValue = contest.ContestValue + 1
    self.ready = false
end

function card:OnPassiveDeactivated(ctx)
    self.ready = false
end

return card
