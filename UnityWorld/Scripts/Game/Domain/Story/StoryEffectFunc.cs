using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 故事效果原子函数注册表（简单轨）
    /// 静态类，维护 funcName → Action 的映射表
    /// 配置文件中 Effects 字段存 {funcName, args}，运行时通过此类查找并执行
    /// </summary>
    public static class StoryEffectFunc
    {
        // ── 注册表 ─────────────────────────────────────────────
        private static readonly Dictionary<string, Action<StoryContext, List<string>>> _registry = new(StringComparer.OrdinalIgnoreCase);

       
        // ── 公共 API ──────────────────────────────────────────

        /// <summary>注册一个原子效果函数</summary>
        public static void Register(string funcName, Action<StoryContext, List<string>> action)
        {
            _registry[funcName] = action;
        }

        /// <summary>
        /// 执行一个效果条目
        /// 函数名不存在时打 Warning 并跳过，不抛出异常
        /// </summary>
        public static void Execute(StoryEffectEntry entry, StoryContext ctx)
        {
            if (_registry.TryGetValue(entry.FuncName, out var action))
            {
                try { action(ctx, entry.Args); }
                catch (Exception e)
                {
                    LogMgr.Warn("[StoryEffectFunc] 执行 '{0}' 时异常：{1}", entry.FuncName, e.Message);
                }
            }
            else
            {
                LogMgr.Warn("[StoryEffectFunc] 未知函数名 '{0}'，已跳过", entry.FuncName);
            }
        }

        /// <summary>执行多个效果条目</summary>
        public static void ExecuteAll(IEnumerable<StoryEffectEntry> entries, StoryContext ctx)
        {
            foreach (var entry in entries)
                Execute(entry, ctx);
        }

        // ── 内置原子函数实现 ──────────────────────────────────

    }
}
