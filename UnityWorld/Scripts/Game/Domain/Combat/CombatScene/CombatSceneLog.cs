using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatScene
    {
        private static bool openLog = true;
        private static CombatScene mainScene;
        private bool haslog;
        private readonly List<string> _trace = new();

        /// <summary>按执行顺序记录本场各角色的 Tick、轮次、行号及动作。</summary>
        public IReadOnlyList<string> TraceLog => _trace.AsReadOnly();

        /// <summary>记录本场 NPC 的动作，同时按日志开关输出到调试日志。</summary>
        public void Trace(CombatNpc npc, string message)
        {
            var line = $"Tick={CurrentTick} Npc={npc.Id} Round={npc.Backpack.Round} Row={npc.Backpack.Row + 1} {message}";
            _trace.Add(line);
            if (openLog) LogMgr.Instance.Dbg(line);
        }

        /// <summary>写入战斗调试日志，每个 Tick 首条消息附带当前 Tick。</summary>
        public static void Log(string msg)
        {
            if (!openLog) return;
            if (mainScene != null && !mainScene.haslog)
            {
                mainScene.haslog = true;
                LogMgr.Instance.Dbg($"[Tick]{mainScene.CurrentTick}");
            }
            LogMgr.Instance.Dbg($"[Combat]{msg}");
        }

        /// <inheritdoc/>
        public override void LogAllInfo() { }

        /// <inheritdoc/>
        public override string ToString() => "";
    }
}
