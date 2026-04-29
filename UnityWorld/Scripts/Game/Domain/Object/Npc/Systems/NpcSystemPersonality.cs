using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 性格子系统：管理 NPC 的八维性格数据
    /// </summary>
    public class NpcSystemPersonality : NpcSystemBase<NpcPersonalityData>
    {
        protected override Dictionary<int, NpcPersonalityData> _dataTable { get; set; } = new();

        /// <summary>
        /// 注册一个 NPC 的性格数据
        /// </summary>
        public override void Register(Npc npc, NpcPersonalityData data)
        {
            _dataTable[npc.Id] = data;
        }

        /// <summary>
        /// 便捷注册：使用默认性格数据
        /// </summary>
        public void Register(Npc npc)
        {
            Register(npc, new NpcPersonalityData());
        }

        /// <summary>
        /// NPC 诞生时：创建性格数据，从 Soul 随机生成八维性格值。
        /// <para>
        /// 八维性格均从 [0, 1) 范围内由 Soul.Rng 随机生成，
        /// 保证同一 NPC 的性格可复现。
        /// </para>
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;

            var personality = new NpcPersonalityData
            {
                Boldness      = npc.Soul.Random(0f, 1f),
                Compassion    = npc.Soul.Random(0f, 1f),
                Greed         = npc.Soul.Random(0f, 1f),
                Honor         = npc.Soul.Random(0f, 1f),
                Rationality   = npc.Soul.Random(0f, 1f),
                Sociability   = npc.Soul.Random(0f, 1f),
                Vengefulness  = npc.Soul.Random(0f, 1f),
                Zeal          = npc.Soul.Random(0f, 1f),
            };

            Register(npc, personality);
        }

        /// <summary>
        /// 每 Tick 更新性格（当前版本无自然变化）
        /// </summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
        }

        /// <summary>
        /// 获取 NPC 的性格数据
        /// </summary>
        public NpcPersonalityData GetPersonality(int npcId)
        {
            if (_dataTable.TryGetValue(npcId, out var data))
            {
                return data;
            }

            LogMgr.Warn("[NpcSystemPersonality] 找不到 NPC {0} 的 PersonalityData，返回默认值", npcId);
            return new NpcPersonalityData();
        }
    }
}
