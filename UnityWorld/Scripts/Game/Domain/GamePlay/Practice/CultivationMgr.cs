using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 功法运行时管理器：管理 NPC 功法持有、修炼进度、节点解锁
    /// （本次仅搭建骨架，不实现生成逻辑和 Tick 逻辑）
    /// </summary>
    public class CultivationMgr : IGameplayMgrBase,ISoulBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static CultivationMgr Instance { get; private set; }


        // ── IDomainMgrBase 属性 ─────────────────────────────
        public string Name => "CultivationMgr";
        public string Desc => "功法玩法管理器，管理修炼相关函数";

        public SoulData Soul {get;}

        // ── 构造函数 ─────────────────────────────────────────
        public CultivationMgr(int seed)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }



        
        /// <summary>
        /// 为 NPC 添加功法，同时激活并设为当前修炼功法（如果当前没有）。
        /// 添加后遍历功法节点，对已解锁的 Card 类型节点自动发牌到 NPC 卡组。
        /// </summary>
        public void AddCultivation(Npc npc, string defineId, int currentPoint = -1)
        {
            var data = npc.CultivationData;
            if (data == null) return;

            // 查找功法定义
            var define = CultivationDefineMgr.Instance?.Get(defineId);
            if (define == null)
            {
                LogMgr.Warn("[CultivationMgr] 找不到功法定义：{0}", defineId);
                return;
            }

            // 如果未指定修炼点数，默认为满（maxPoint），即所有节点解锁
            var point = currentPoint < 0 ? define.MaxPoint : currentPoint;

            var slot = new CultivationSlot
            {
                DefineId = defineId,
                CurrentPoint = point
            };
            data.GongFaData.AllSlots.Add(slot);
            data.GongFaData.ActiveSlots.Add(slot);

            // 如果当前没有修炼功法，自动设为当前修炼功法
            if (data.PracticeData.NowCultivationSlot == null)
            {
                data.PracticeData.NowCultivationSlot = slot;
            }

            // 遍历功法节点，对已解锁的 Card 类型节点发牌
            GrantCardsFromCultivation(npc, define, point);
        }

        /// <summary>
        /// 遍历功法节点，对已解锁的 Card 类型节点自动发牌到 NPC 卡组
        /// </summary>
        private void GrantCardsFromCultivation(Npc npc, CultivationDefine define, int currentPoint)
        {
            
        }


        // ── 生命周期方法（骨架占位）─────────────────────────

        public void Init()
        {
            LogMgr.Dbg("[CultivationMgr] 初始化完成（骨架模式）");
        }

        public void Begin()
        {
            // 依赖 NpcMgr、RealmDefineMgr、CultivationDefineMgr 已初始化
        }

        public void Tick(float deltaTime)
        {
            // 修炼速度计算、元气关联、节点解锁等逻辑待实现
        }

        public void Update()
        {
            // 轻量帧回调
        }

        public void Render(float dt)
        {
            // 渲染更新
        }

        public void End()
        {
            Instance = null;
        }


        public void Log()
        {
            
        }
    }
}