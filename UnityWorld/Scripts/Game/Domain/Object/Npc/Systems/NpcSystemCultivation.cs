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
        /// <summary>按角色编号保存的修行数据</summary>
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
            // 如果当前没有修炼功法，设为当前
            if (data.PracticeData.GetNowGongFa() == null)
            {
                data.PracticeData.NowGongFaCardId = gongFa.Id;
            }
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
                LogMgr.Instance.Warn("[NpcSystemCultivation] 无法设定当前功法 {0}，因为它未激活", gongFa.DefineId);
                return;
            }
            data.PracticeData.NowGongFaCardId = gongFa.Id;
        }
        /// <summary>
        /// NPC 诞生时：创建修行数据，应用预置属性和功法，从 Soul 计算五行亲和
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            // 创建并注册修行数据
            var data = new NpcCultivationData();
            data.PracticeData.CurrentRealmLevel = context.GetValue("InitCultivationLevel", 0);
            Register(npc, data);

            
            data.Affinity = new ElementalAffinity(npc.Soul);
            npc.Stats.SetBase("AffinityJin", data.Affinity.Jin);
            npc.Stats.SetBase("AffinityMu", data.Affinity.Mu);
            npc.Stats.SetBase("AffinityShui", data.Affinity.Shui);
            npc.Stats.SetBase("AffinityHuo", data.Affinity.Huo);
            npc.Stats.SetBase("AffinityTu", data.Affinity.Tu);

            // InitStat 写入（来自 NpcDefine 或外部预设）
            var initStat = context.Get<Dictionary<string, int>>("InitStat");
            if (initStat != null)
            {
                foreach (var kv in initStat)
                    npc.Stats.SetBase(kv.Key, kv.Value);
            }
            
            // InitGongFa：从 NpcDefine 预配的功法列表，逐一装配
            var initGongFa = context.Get<List<InitGongFaEntry>>("InitGongFa");
            if (context.AssemblySource != NpcAssemblySource.Template && initGongFa != null && initGongFa.Count > 0)
            {
                foreach (var entry in initGongFa)
                {
                    CultivationMgr.Instance.AddCultivation(npc, entry.DefineId, entry.CurrentPoint);
                }
            }


        }

        /// <summary>保留角色修行数据的时步入口</summary>
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
