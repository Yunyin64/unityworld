-- ══════════════════════════════════════════════════════════════
-- Condition.lua
-- 条件判断类包装函数（对应 CombatBaseCondition.cs）
-- ══════════════════════════════════════════════════════════════

--- 通用关键词判断
--- @param keywordName string API名称
--- @param ctx APIContext
--- @param card CombatCard 目标卡牌
--- @return boolean
function KeywordCheck(keywordName, ctx, card)
    ctx:Set("Target", card)
    API:Execute(keywordName, ctx)
    return ctx:GetObject("Result")
end

function IsFabao(ctx, card)   return KeywordCheck("IsFabao", ctx, card) end
function IsFaShu(ctx, card)   return KeywordCheck("IsFaShu", ctx, card) end
function IsGongFa(ctx, card)  return KeywordCheck("IsGongFa", ctx, card) end
function IsItem(ctx, card)    return KeywordCheck("IsItem", ctx, card) end
function IsEquip(ctx, card)   return KeywordCheck("IsEquip", ctx, card) end
function IsZhaoShi(ctx, card) return KeywordCheck("IsZhaoShi", ctx, card) end
