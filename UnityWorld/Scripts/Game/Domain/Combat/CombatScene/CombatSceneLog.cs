using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatScene
    {
        /// <summary>是否开启战斗日志输出</summary>
        private static bool openLog = true;
        /// <summary>静态日志使用的主战场</summary>
        private static CombatScene mainScene;
        /// <summary>当前时步的日志表头是否已输出</summary>
        private bool haslog;
        /// <summary>本场战斗的顺序追踪记录</summary>
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
        /// <summary>保留完整战场日志输出入口</summary>
        public override void LogAllInfo() { }

        /// <inheritdoc/>
        /// <summary>返回战斗场景字符串，当前为空</summary>
        public override string ToString() => "";
    }
}
