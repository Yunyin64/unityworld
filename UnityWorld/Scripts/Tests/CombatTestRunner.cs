using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;
using CardDefineList = System.Collections.Generic.List<UnityWorld.Game.Data.CardDefine>;

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
            LogMgr.Dbg("=== CombatTestRunner.RunBasicTest ===");

            APIMgr.Instance.ExportDoc();

            // ── 创建两个真实 NPC ─────────────────────────────
            var npcMgr = NpcMgr.Instance;
            if (npcMgr == null)
            {
                LogMgr.Dbg("[CombatTestRunner] 错误：NpcMgr 未初始化");
                return;
            }

            var ctxA = new BirthContext();
            ctxA.Set("Path", PracticePath.Wu);
            var npcA = npcMgr.Birth(ctxA);

            var ctxB = new BirthContext();
            ctxB.Set("Path", PracticePath.Ling);
            var npcB = npcMgr.Birth(ctxB);

            LogMgr.Dbg($"  NPC A: ID={npcA.Id}, HP={npcA.GetHpMax()}, SP={npcA.GetSpMax()}, MP={npcA.GetMpMax()}");
            LogMgr.Dbg($"  NPC B: ID={npcB.Id}, HP={npcB.GetHpMax()}, SP={npcB.GetSpMax()}, MP={npcB.GetMpMax()}");

            // ── 为 NPC 添加功法获得卡组 ──────────────────────
            CultivationMgr.Instance.AddCultivation(npcA, "ling_golden_blade");
            CultivationMgr.Instance.AddCultivation(npcA, "wu_stone_body");

            CultivationMgr.Instance.AddCultivation(npcB, "ling_flame_heart");
            CultivationMgr.Instance.AddCultivation(npcB, "hun_frost_mind");

            npcA.AssignAllToField();
            npcB.AssignAllToField();

            // ── 发起战斗 ─────────────────────────────────────
            var result = CombatMgr.Instance.RunCombat(npcA, npcB);


            LogMgr.Dbg($"\n=== 战斗结束 ===");
            if (result != null)
            {
                LogMgr.Dbg($"  结束原因: {result.GetValue("EndReason")}");
                LogMgr.Dbg($"  胜方: {result.GetValue("WinnerTeam")}");
                LogMgr.Dbg($"  总 Tick: {result.GetValue("TotalTicks")}");
            }
            LogMgr.Dbg("=== 测试完成 ===");
        }
    }
}