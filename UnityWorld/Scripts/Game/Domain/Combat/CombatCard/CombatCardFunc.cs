using NLua;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard 
    {
        
        /// <summary>
        /// 在 Start 阶段检查并初始化 Lua 卡牌：
        /// 加载 .lua 脚本 → env = return 的 card table。
        /// </summary>
        public void InitializeLuaCards()
        {
            var luaMgr = LuaMgr.Instance;

            // 加载 Lua 脚本，获得独立的 card table
            env = luaMgr.LoadCardScript(DefineId);
            if(env == null)
            {
                Log($"  Lua 卡牌脚本加载失败: {DefineId}.lua");
                return;
            }else{
                Log($"  Lua 卡牌脚本加载成功: {DefineId}.lua");
            }
            // env 为 null 说明没有对应 lua 文件，正常情况
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

            Log($"  [{Owner.GetName()}] 卡[{DisplayName}] 入槽: {contestData}");

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