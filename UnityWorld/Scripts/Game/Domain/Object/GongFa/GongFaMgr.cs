using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// GongFa 运行时管理器：扁平全局表（cardId → GongFa），
    /// 负责所有 GongFa 实例的 Add/Remove/Get/全局遍历。
    /// </summary>
    public class GongFaMgr : DomainMgrBase<GongFa>,ISoulBase
    {
        /// <summary>单例</summary>
        public static GongFaMgr Instance { get; private set; }

        public SoulData Soul { get; set; }     


        public GongFaMgr(int seed = 12345)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }


        // ── 生命周期 ────────────────────────────────────────

        public override void Init() { }

        public override void Begin() { }

        public override void Tick(float deltaTime) { }

        public override void Update() { }

        public override void Render(float dt) { }

        public override void End()
        {
            _allEntities.Clear();
            Instance = null;
        }

        public override IEnumerator Save() { yield break; }

        public override IEnumerator Load() { yield break; }

        public GongFa InstantiateFromDefine(int id,CultivationDefine define)
        {
            // 创建 GongFa 实例，Id = card.Id
            var slot = new GongFa
            {
                Id = id,
                DefineId = define.ID,
                DisplayName = define.DisplayName,
                CurrentPoint = 0
            };
            // 注册到 GongFaMgr 全局表
            GongFaMgr.Instance?.Add(slot.Id, slot);

            return  slot;
        }
        /// <summary>日志输出</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[GongFaMgr] 功法运行时管理器 | GongFa={0}", Count);
        }
    }
}
