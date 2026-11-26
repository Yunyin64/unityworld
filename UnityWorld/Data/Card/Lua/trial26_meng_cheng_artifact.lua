-- 分潮盘。激活期间，己方每次实际抽出灵元，下一次射击拼点 +1。
local card = setmetatable({}, { __index = CardBase })

function card:OnManaDraw(ctx)
    if ctx.Caster ~= self.m_Owner then return end
    AddNpcBuff(ctx, "Self", "SheJi_Changed_Once", 1, -1)
end

return card
