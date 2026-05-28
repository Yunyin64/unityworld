using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Equip 运行时管理器：扁平全局表（cardId → Equip），
    /// 负责所有 Equip 实例的 Add/Remove/Get/全局遍历。
    /// </summary>
    public class EquipMgr : DomainMgrBase<Equip>,ISoulBase
    {
        /// <summary>单例</summary>
        public static EquipMgr Instance { get; private set; }

        public SoulData Soul { get; set; }


        public EquipMgr(int seed = 12345)
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

        /// <summary>日志输出</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[EquipMgr] 装备运行时管理器 | Equip={0}", Count);
        }
    }
}
