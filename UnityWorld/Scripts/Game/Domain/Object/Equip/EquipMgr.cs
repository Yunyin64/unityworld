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

        /// <summary>管理器的基础实体数据</summary>
        public SoulData Soul { get; set; }


        /// <summary>按种子初始化实体数据并登记管理器</summary>
        public EquipMgr(int seed = 12345)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }


        // ── 生命周期 ────────────────────────────────────────

        /// <summary>保留管理器初始化入口</summary>
        public override void Init() { }

        /// <summary>保留世界启动回调</summary>
        public override void Begin() { }

        /// <summary>保留时步回调</summary>
        public override void Tick(float deltaTime) { }

        /// <summary>保留帧更新回调</summary>
        public override void Update() { }

        /// <summary>保留渲染回调</summary>
        public override void Render(float dt) { }

        /// <summary>清空装备实例并释放管理器</summary>
        public override void End()
        {
            _allEntities.Clear();
            Instance = null;
        }

        /// <summary>结束存档流程，当前无写入</summary>
        public override IEnumerator Save() { yield break; }

        /// <summary>结束读档流程，当前无读取</summary>
        public override IEnumerator Load() { yield break; }

        /// <summary>日志输出</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[EquipMgr] 装备运行时管理器 | Equip={0}", Count);
        }
    }
}
