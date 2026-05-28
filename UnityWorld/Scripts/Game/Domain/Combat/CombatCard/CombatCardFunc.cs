using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard 
    {
        
        /// <summary>
        /// 在 Start 阶段检查并初始化 Lua 卡牌：
        /// 加载 .lua 脚本 → env = return 的 card table → 预扫描 LuaHooks。
        /// </summary>
        public void InitializeLuaCards()
        {
            // 加载 Lua 脚本，获得独立的 card table
            env = LuaMgr.Instance.LoadCardScript(CardDefineMgr.Instance.Get(DefineId));
            // 注入 C# 卡牌实例引用，供 Lua 通过 self._self 访问
            if (env != null) env["_self"] = this;
            // 预扫描 Lua 函数缓存
            LuaHooks = LuaMgr.ScanLuaHooks(env);

            // 从 Lua CardData/Keywords 覆写 BaseData
            ApplyLuaOverrides();
        }

        /// <summary>
        /// 从 Lua env 中读取 CardData / Keywords，覆写 C# 侧 BaseData。
        /// 在 InitializeLuaCards 之后、CallLua("OnPreStart") 之前调用。
        /// </summary>
        private void ApplyLuaOverrides()
        {
            if (env == null) return;
            var baseData = BaseData;
            if (baseData == null) return;

            // ── 覆写 CardData，先不处理 TODO────────────────────────────────────
            /*
            if (env["CardData"] is LuaTable cardData)
            {
                if (cardData["Size"] is long size)
                    baseData.Size = (int)size;

                if (cardData["Cooldown"] is long cd)
                    baseData.Cooldown = cd;
                else if (cardData["Cooldown"] is double cdD)
                    baseData.Cooldown = (float)cdD;

                if (cardData["ManaCost"] is LuaTable manaTbl)
                {
                    baseData.ManaCost.Clear();
                    foreach (var key in manaTbl.Keys)
                    {
                        var elemStr = key.ToString();
                        if (Enum.TryParse<BaseElementType>(elemStr, out var baseElem))
                        {
                            var val = manaTbl[key];
                            int cost = val is long l ? (int)l : (int)(double)val;
                            baseData.ManaCost[new ElementType(baseElem)] = cost;
                        }
                    }
                }
            }
            
            */
            // ── 覆写 Keywords ────────────────────────────────────
            // nil 或空表不修改，保留 C# 侧已有 Keywords
            if (env["Keywords"] is LuaTable kwTbl && kwTbl.Values.Count > 0)
            {
                foreach (var val in kwTbl.Values)
                {
                    var kw = val.ToString();
                    if (!baseData.Keywords.Contains(kw))
                        baseData.Keywords.Add(kw);
                }
            }

            // CardData 里的 CardType 字段也视为 keyword（兼容旧写法）
            if (env["CardData"] is LuaTable cd2 && cd2["CardType"] is string cardType)
            {
                if (!string.IsNullOrEmpty(cardType) && !baseData.Keywords.Contains(cardType))
                    baseData.Keywords.Add(cardType);
            }
        }

        
        /// <summary>
        /// 尝试将卡推入待发槽。
        /// 构造 ContestData 快照后放入 PendingSlot。
        /// </summary>
        /// <param name="contestType">拼点类型（Zhan/Ci/Da/SheJi/Shield/Block）</param>
        /// <param name="element">元素类型（仅攻击类有效）</param>
        /// <param name="contestValue">拼点数值（攻击值/盾值/防值）</param>
        public void TryPushToPendingSlot(ContestType contestType, ElementType element, float contestValue)
        {
            // 构造 ContestData 快照
            var contestData = new ContestData
            {
                ContestType = contestType,
                Element = element,
                ContestValue = contestValue,
                SourceCard = this
            };

            // Modifier 修正拼点数值
            Owner.ModifyContest(contestData);

            // 入槽即切换阶段，防止下一 Tick 再次被收集
            SetPhase(CombatCardPhase.InPending);

            // 待发槽空 → ContestData 入槽
            Owner.AddContestData(contestData);

            Log($"  入槽: {contestData}  [{Owner.GetName()}] ");

            // 待发槽满 → 由 CombatNpc.ProcessContest() 统一处理挤出与对拼
        }
        public void ResetCD()
        {
            //如果有弹药机制且弹药不足，则不重置CD    
            if(IsAmount ){
                if (HasAmount) ConsumeAmount();
                else return;
            }
            if(Ticks["CD"] >= GetCDMax()){
            Ticks["CD"] = 0;
            SetPhase(CombatCardPhase.Ready);
            }
        }
        /// <summary>
        /// 没有消耗直接进 CD，有消耗则尝试扣除、成功也进 CD，失败什么都不做。
        /// </summary>
        public void CheckMana()
        {
            var cost = GetCombatManaCost();
            if (cost.Count == 0 || Owner.TryCostMana(cost)) SetPhase(CombatCardPhase.InCD);
        }
        public void Charge(int Tick)
        {
            var old = Ticks["CD"];
            Ticks["CD"] = Math.Min(Ticks["CD"]+Tick,GetCDMax());
            Log($"  [{Owner.GetName()}] 充能: {old} +{Tick}-> {Ticks["CD"]} | {GetCDMax()}  ");
        }
    }
}