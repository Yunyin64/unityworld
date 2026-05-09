using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 功法运行时管理器：管理 NPC 功法持有、修炼进度、节点解锁
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
            // 查找功法定义
            var define = CultivationDefineMgr.Instance.Get(defineId);
            if (define == null)
            {
                LogMgr.Warn("[CultivationMgr] 找不到功法定义：{0}", defineId);
                return;
            }
            // 如果未指定修炼点数，默认为满（maxPoint），即所有节点解锁
            var point = currentPoint < 0 ? define.MaxPoint : currentPoint;
            var slot = new GongFa 
            {
                DefineId = defineId,
                CurrentPoint = point
            };
            npc.AddGongFa(slot);

            // 遍历功法节点，对已解锁的 Card 类型节点发牌
            GrantCardsFromCultivation(npc, define, point);
        }

        /// <summary>
        /// 移除 NPC 持有的某个功法槽位。
        /// 同时从激活列表中移除；若当前修炼的正好是该功法则清空。
        /// </summary>
        public void RemoveCultivation(Npc npc, string defineId)
        {

            var slot = npc.GetAllSlots().FirstOrDefault(s => s.DefineId == defineId);
            if (slot == null) return;
            npc.RemoveGongFa(slot);

            LogMgr.Dbg("[CultivationMgr] {0} 失去功法 {1}", npc, defineId);
        }

        /// <summary>
        /// 为 NPC 当前修炼功法增加修炼点数，解锁达到阈值的新节点并发牌。
        /// </summary>
        /// <returns>实际增加的点数（已满则返回 0）</returns>
        public int AddProgress(Npc npc, int amount)
        {

            var slot = npc.GetNowGongFaData();
            if (slot == null) return 0;

            var define = CultivationDefineMgr.Instance?.Get(slot.DefineId);
            if (define == null) return 0;

            int oldPoint = slot.CurrentPoint;
            int newPoint = System.Math.Min(oldPoint + amount, define.MaxPoint);
            int delta = newPoint - oldPoint;
            if (delta <= 0) return 0;

            slot.CurrentPoint = newPoint;

            // 检查新解锁的节点并发牌
            GrantCardsForRange(npc, define, oldPoint, newPoint);

            LogMgr.Dbg("[CultivationMgr] {0} 功法 {1} 进度 {2} → {3}",
                npc, slot.DefineId, oldPoint, newPoint);

            return delta;
        }

        /// <summary>
        /// 切换 NPC 当前修炼的功法
        /// </summary>
        public bool SwitchCultivation(Npc npc, string defineId)
        {
            var slot = npc.GetAllSlots().FirstOrDefault(s => s.DefineId == defineId);
            if (slot == null)
            {
                LogMgr.Warn("[CultivationMgr] {0} 未激活功法 {1}，无法切换", npc, defineId);
                return false;
            }
            npc.SetNowGongFa(slot);
            return true;
        }

        // ── 内部：发牌逻辑 ──────────────────────────────────

        /// <summary>
        /// 遍历功法节点，对已解锁的 Card 类型节点自动发牌到 NPC 卡组
        /// </summary>
        private void GrantCardsFromCultivation(Npc npc, CultivationDefine define, int currentPoint)
        {
            GrantCardsForRange(npc, define, -1, currentPoint);
        }

        /// <summary>
        /// 对 (oldPoint, newPoint] 范围内解锁的 Card 节点发牌
        /// </summary>
        private void GrantCardsForRange(Npc npc, CultivationDefine define, int oldPoint, int newPoint)
        {
            if (define.Points == null) return;
            var cardMgr = CardMgr.Instance;
            if (cardMgr == null) return;


            foreach (var pt in define.Points)
            {
                if (pt.Threshold > newPoint) continue;
                if (pt.Threshold <= oldPoint) continue;

                if (pt.Type == CultivationPointType.Card && !string.IsNullOrEmpty(pt.RefId))
                {  
                    npc.GainCard(pt.RefId);
                    LogMgr.Dbg("[CultivationMgr] {0} 功法解锁卡牌 {1}", npc, pt.RefId);
                    
                }
                // 后续扩展：BehaviorCard / Modifier / Story 类型在此追加分支
            }
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
            LogMgr.Dbg("┌── CultivationMgr · {0} ──────────────────────────", Name);
            LogMgr.Dbg("│  {0}", Desc);
            LogMgr.Dbg("│  功法定义总数: {0}",
                CultivationDefineMgr.Instance?.GetAll().Count() ?? 0);
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }
}