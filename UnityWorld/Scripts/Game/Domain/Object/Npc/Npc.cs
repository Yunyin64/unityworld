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

        public Npc(int guid)
        {
            Soul = new SoulData(guid);
            Stats = StatMgr.Instance.CreateBlock(Id,GetType());
        }
        protected NpcBioData BioData           => NpcMgr.Instance.BioSystem.GetData(Id);
        protected NpcBehaviorData BehaviorData     => NpcMgr.Instance.BehaviorSystem.GetData(Id);
        protected NpcCultivationData CultivationData           => NpcMgr.Instance.CultivationSystem.GetData(Id);
        protected NpcCardData CardData           => NpcMgr.Instance.CardSystem.GetData(Id);
        protected NpcPersonalityData PersonalityData           => NpcMgr.Instance.PersonalitySystem.GetData(Id);
         
        public SoulData Soul {get;set;}
        
        public override void LogAllInfo()
        {
            BioData.Log();
            BehaviorData.Log();
        }

        public override string ToString()
        {
            return  string.Format("Npc[{0}{1}]",Id,GetName());
        }
    }
}