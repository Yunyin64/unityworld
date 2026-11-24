using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    public class NpcSystemBio : NpcSystemBase<NpcBioData>
    {   
        /// <summary>按角色编号保存的生理数据</summary>
        protected override Dictionary<int, NpcBioData> _dataTable { get;set; } = new();

        /// <summary>保留角色生理的时步入口</summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            
        }
        
        /// <summary>更新姓与名，并按需更新字和道号</summary>
        public string ReName(NpcBioData data,string Surname,string GivenName,string CourtesyName = "" ,string DaoTitle = "")
        {
            data.NameData.Surname = Surname;
            data.NameData.GivenName = GivenName;
            if(!string.IsNullOrWhiteSpace(CourtesyName))data.NameData.CourtesyName = CourtesyName;
            if(!string.IsNullOrWhiteSpace(DaoTitle))data.NameData.DaoTitle = DaoTitle;

            return Surname+GivenName;
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

    public partial class Npc
    {
        /// <summary>更新角色的姓与名，返回拼接后的姓名</summary>
        public string ReName(string Surname,string GivenName,string CourtesyName = "" ,string DaoTitle = "") 
        => NpcMgr.Instance.BioSystem.ReName(BioData, Surname,GivenName);
    }
}