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


        /// <summary>为 NPC 添加一个功法（通过 GongFa 实例，使用 gongFa.Id 操作索引）</summary>
        public void AddGongFa(NpcCultivationData data, GongFa gongFa)
        {
            data.GongFaData.AllSlotCardIds.Add(gongFa.Id);
            data.GongFaData.ActiveSlotCardIds.Add(gongFa.Id);
        }

        /// <summary>为 NPC 移除一个功法（通过 GongFa 实例，使用 gongFa.Id 操作索引）</summary>
        public void RemoveGongFa(NpcCultivationData data, GongFa gongFa)
        {
            data.GongFaData.AllSlotCardIds.Remove(gongFa.Id);
            data.GongFaData.ActiveSlotCardIds.Remove(gongFa.Id);
        }

        /// <summary>设置 NPC 当前修炼的功法（通过 GongFa 实例）</summary>
        public void SetNowGongFa(NpcCultivationData data, GongFa gongFa)
        {
            if (!data.GongFaData.ActiveSlotCardIds.Contains(gongFa.Id))
            {
                LogMgr.Warn("[NpcSystemCultivation] 无法设定当前功法 {0}，因为它未激活", gongFa.DefineId);
                return;
            }
            data.PracticeData.NowGongFaCardId = gongFa.Id;
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
        /// <summary>添加功法（接收 GongFa 实例）</summary>
        public void AddGongFa(GongFa gongFa) => NpcMgr.Instance.CultivationSystem.AddGongFa(CultivationData, gongFa);

        /// <summary>移除功法（接收 GongFa 实例）</summary>
        public void RemoveGongFa(GongFa gongFa) => NpcMgr.Instance.CultivationSystem.RemoveGongFa(CultivationData, gongFa);

        /// <summary>设置当前修炼功法（接收 GongFa 实例）</summary>
        public void SetNowGongFa(GongFa gongFa) => NpcMgr.Instance.CultivationSystem.SetNowGongFa(CultivationData, gongFa);
    }
}
