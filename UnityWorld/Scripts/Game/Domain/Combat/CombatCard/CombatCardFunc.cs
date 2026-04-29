using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard 
    {
        
        /// <summary>
        /// 在 Start 阶段检查并初始化 Lua 卡牌：
        /// 加载 .lua 脚本 → 标记 IsLuaCard → 应用 Keywords → 注册被动 Hook。
        /// </summary>
        public void InitializeLuaCards()
        {
            var luaMgr = LuaMgr.Instance;
            if (luaMgr == null) return;

             if (!luaMgr.HasCardScript(DefineId)) return;

                    // 加载 Lua 脚本
                    env = luaMgr.LoadCardScript(DefineId);
                    if (env == null) return;

                    // 读取 Lua 中的 Keywords 声明并应用
                    //ApplyLuaKeywords(npc, cardState, env);

                    // 发现并注册被动 Hook
                    var hooks = luaMgr.DiscoverHooks(env);
                    foreach (var hookName in hooks)
                    {
                        if (hookName == "OnUse") continue; // OnUse 由框架直接调用

                        if (!LuaMgr.HookToEventId.TryGetValue(hookName, out var eventId))
                        {
                            continue;
                        }

                        var scope = new ScopeKey(Scope.CombatNpc, Owner.Id.ToString());
                        var listenerKey = $"Lua_{Owner.Id}_{DefineId}_{hookName}";

                        //注册trigger事件todo

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

            // 入槽即切换阶段，防止下一 Tick 再次被收集
            Phase = CombatCardPhase.InPending;

            // 待发槽空 → ContestData 入槽
            Owner.AddContestData(contestData);

            CombatScene.Log($"  [{Owner.GetName()}] 卡[{DisplayName}] 入槽: {contestData}");

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
            Phase = CombatCardPhase.Ready;
            }
        }
        /// <summary>
        /// 没有消耗直接进 CD，有消耗则尝试扣除、成功也进 CD，失败什么都不做。
        /// </summary>
        public void CheckMana()
        {
            var cost = GetCombatManaCost();
            if (cost.Count == 0 || Owner.TryCostMana(cost))
            Phase = CombatCardPhase.InCD;
        }
        public void Charge(int Tick)
        {
            Ticks["CD"] += Tick;
        }
        public void AddCardBuff()
        {
            
        }
    }
}