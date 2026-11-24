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

        /// <summary>管理器的基础实体数据</summary>
        public SoulData Soul { get; set; }     


        /// <summary>按种子初始化实体数据并登记管理器</summary>
        public GongFaMgr(int seed = 12345)
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

        /// <summary>清空功法实例并释放管理器</summary>
        public override void End()
        {
            _allEntities.Clear();
            Instance = null;
        }

        /// <summary>结束存档流程，当前无写入</summary>
        public override IEnumerator Save() { yield break; }

        /// <summary>结束读档流程，当前无读取</summary>
        public override IEnumerator Load() { yield break; }

        /// <summary>按配置创建功法并使用指定编号注册实例</summary>
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
