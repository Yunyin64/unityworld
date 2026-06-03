using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    public class NpcSystemBio : NpcSystemBase<NpcBioData>
    {   
        protected override Dictionary<int, NpcBioData> _dataTable { get;set; } = new();

        public override void OnTick(Npc npc, float deltaTime)
        {
            
        }

        /// <summary>
        /// NPC 诞生时初始化 BioData：从 ctx kv 读取姓名/性别，填充所有基础生物信息并注册
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;

            var data = new NpcBioData
            {
                Gender = context.GetEmValue<NpcTypes.Gender>("Gender"),
                NpcType = context.GetEmValue<NpcTypes.NpcType>("NpcType", NpcTypes.NpcType.Human),
                IsAlive = true,
                AgeAccumulated = 0f,
                BirthTick = 0,
                BaseMoveSpeed = 3f,
                NameData = new NpcNameData
                {
                    Surname = context.GetValue("Surname"),
                    GivenName = context.GetValue("GivenName"),
                    DaoTitle = context.GetValue("DaoTitle"),
                },
                AppearanceData = new NpcAppearanceData
                {
                    Height = npc.Soul.Random(155f, 190f),
                },
            };

            Register(npc, data);
        }
    }
}