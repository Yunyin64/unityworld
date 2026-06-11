using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 战斗系统测试入口：加载真实 JSON 卡牌数据，跑完整战斗流程，输出日志。
    /// 依赖 GameDataMgr（数据层）+ CardMgr（运行时实例化）+ APIMgr + EventMgr。
    /// </summary>
    public static class CombatTestRunner
    {
        /// <summary>
        /// 运行基础战斗测试：双方各带真实卡牌，跑到结束，导出日志。
        /// </summary>
        public static void RunBasicTest()
        {
            LogMgr.Instance.Dbg("=== CombatTestRunner.RunBasicTest ===");

            APIMgr.Instance.ExportDoc();

            // ── 创建两个真实 NPC ─────────────────────────────
            var npcMgr = NpcMgr.Instance;
            if (npcMgr == null)
            {
                LogMgr.Instance.Dbg("[CombatTestRunner] 错误：NpcMgr 未初始化");
                return;
            }

            // NpcA/B 从 JSON 定义读取（Data/Npc/Test.json）
            var defineA = NpcDefineMgr.Instance.Get("test_npc_a");
            var defineB = NpcDefineMgr.Instance.Get("test_npc_b");
            if (defineA == null || defineB == null)
            {
                LogMgr.Instance.Dbg("[CombatTestRunner] 错误：找不到 test_npc_a 或 test_npc_b 定义");
                return;
            }
            var npcA = npcMgr.Assemble(defineA);
            var npcB = npcMgr.Assemble(defineB);

            // NpcC/D 从 Monster 定义读取（Data/Npc/Monster.json）
            var NpcC = npcMgr.Assemble(NpcDefineMgr.Instance.Get("monster_wolf_0"));
            var NpcD = npcMgr.Assemble(NpcDefineMgr.Instance.Get("monster_wolf_0"));

            // ── 手动装备法宝：card_fabao_jian + long_sword ──────
            var fabaoCard = npcA.GainEquip("card_fabao_jian", "long_sword");
            npcA.EquipFaBao(fabaoCard.Id);

            npcA.AssignAllToField();
            npcB.AssignAllToField();
            NpcC.AssignAllToField();
            NpcD.ReName("","灰狼2");
            NpcD.AssignAllToField();

            
            // ── 发起战斗 ─────────────────────────────────────
            var result = CombatMgr.Instance.RunCombat(npcA,new [] {NpcC,NpcD});


            LogMgr.Instance.Dbg($"\n=== 战斗结束 ===");
            if (result != null)
            {
                LogMgr.Instance.Dbg($"  结束原因: {result.GetValue("EndReason")}");
                LogMgr.Instance.Dbg($"  胜方: {result.GetValue("WinnerTeam")}");
                LogMgr.Instance.Dbg($"  总 Tick: {result.GetValue("TotalTicks")}");
            }
            LogMgr.Instance.Dbg("=== 测试完成 ===");
        }
    }
}