using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC核心实体（轻量句柄，真实数据由各子系统管理）
    /// </summary>
    public partial class Npc:GameEntityBase,ISoulBase
    {
        /// <summary>唯一ID</summary>
        public int Id =>Soul.Guid;

        /// <summary>以实体编号建立角色灵魂数据和属性表</summary>
        public Npc(int guid)
        {
            Soul = new SoulData(guid);
            Stats = StatMgr.Instance.CreateBlock(Id,GetType());
        }
        /// <summary>角色生理和姓名数据</summary>
        protected NpcBioData BioData           => NpcMgr.Instance.BioSystem.GetData(Id);
        /// <summary>角色当前行为数据</summary>
        protected NpcBehaviorData BehaviorData     => NpcMgr.Instance.BehaviorSystem.GetData(Id);
        /// <summary>角色修行和功法数据</summary>
        protected NpcCultivationData CultivationData           => NpcMgr.Instance.CultivationSystem.GetData(Id);
        // 战斗子类覆盖为本场卡实例与布局，继承的只读入口也不会查询世界仓库。
        /// <summary>角色持卡和出战选择数据</summary>
        protected virtual NpcCardData CardData => NpcMgr.Instance.CardSystem.GetData(Id);
        /// <summary>角色备战背包布局</summary>
        protected virtual NpcBackpackData BackpackData => NpcMgr.Instance.BackpackSystem.GetData(Id);
        /// <summary>角色性格数据</summary>
        protected NpcPersonalityData PersonalityData           => NpcMgr.Instance.PersonalitySystem.GetData(Id);
         
        /// <summary>角色的基础实体数据</summary>
        public SoulData Soul {get;set;}
        
        /// <summary>输出角色生理和行为数据</summary>
        public override void LogAllInfo()
        {
            BioData.Log();
            BehaviorData.Log();
        }

        /// <summary>格式化角色编号与姓名</summary>
        public override string ToString()
        {
            return  string.Format("Npc[{0}{1}]",Id,GetName());
        }
    }
}
