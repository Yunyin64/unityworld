using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 修行数据系统：管理 NPC 的修行总数据（NpcCultivationData）
    /// 
    /// 职责：
    ///   - 管理 NpcCultivationData 的注册/查询（道途、寿元、属性、五行、功法数据、修炼进度）
    /// </summary>
    public class NpcSystemCultivation : NpcSystemBase<NpcCultivationData>
    {
        protected override Dictionary<int, NpcCultivationData> _dataTable { get; set; } = new();


        /// <summary>获取 NPC 的修行数据</summary>
        public NpcCultivationData? GetCultivation(int id)
            => _dataTable.TryGetValue(id, out var data) ? data : null;

        /// <summary>获取 NPC 的修行数据（通过 Npc 实体）</summary>
        public NpcCultivationData? GetCultivation(Npc npc)
            => GetCultivation(npc.Id);


        public void AddGongFa(NpcCultivationData data, GongFa  gongFa)
        {
            data.GongFaData.AllSlots.Add(gongFa);
            data.GongFaData.ActiveSlots.Add(gongFa);
        }

        
        public void RemoveGongFa(NpcCultivationData data, GongFa  gongFa)
        {
            data.GongFaData.AllSlots.Remove(gongFa);
            data.GongFaData.ActiveSlots.Remove(gongFa);
        }

        public void SetNowGongFa(NpcCultivationData data, GongFa gongFa)
        {
            if (!data.GongFaData.ActiveSlots.Contains(gongFa))
            {
                LogMgr.Warn("[NpcSystemCultivation] 无法设定当前功法 {0}，因为它未激活", gongFa.DefineId);
                return;
            }
            data.PracticeData.NowGongFaData = gongFa;
        }
        /// <summary>
        /// NPC 诞生时：创建修行数据，根据 Soul 计算五行亲和，根据八大属性计算战斗三维
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;

            // 创建并注册修行数据
            var data = new NpcCultivationData();
            data.Affinity = new ElementalAffinity(npc.Soul);
            npc.Stats.SetBase("AffinityJin", data.Affinity.Jin);
            npc.Stats.SetBase("AffinityMu", data.Affinity.Mu);
            npc.Stats.SetBase("AffinityShui", data.Affinity.Shui);
            npc.Stats.SetBase("AffinityHuo", data.Affinity.Huo);
            npc.Stats.SetBase("AffinityTu", data.Affinity.Tu);
            Register(npc, data);
        }

        public override void OnTick(Npc npc, float deltaTime)
        {
            // 修行数据的 Tick 逻辑由 NpcSystemPractice 驱动
        }
    }

    public partial class Npc
    {
        public void AddGongFa( GongFa gongFa) => NpcMgr.Instance.CultivationSystem.AddGongFa(CultivationData, gongFa);
        public void RemoveGongFa( GongFa gongFa) => NpcMgr.Instance.CultivationSystem.RemoveGongFa(CultivationData, gongFa);
        public void SetNowGongFa( GongFa gongFa) => NpcMgr.Instance.CultivationSystem.SetNowGongFa(CultivationData, gongFa);
    }
}
