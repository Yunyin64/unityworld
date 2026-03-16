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

        // ── 静态构造：注册所有内置函数 ──────────────────────────
        static StoryEffectFunc()
        {
            Register("GiveTrait",         ExecGiveTrait);
            Register("RemoveTrait",       ExecRemoveTrait);
            Register("GiveActionCard",    ExecGiveActionCard);
            Register("ModifyAura",        ExecModifyAura);
            Register("ModifyStat",        ExecModifyStat);
            Register("TriggerStory",      ExecTriggerStory);
            Register("TriggerStoryByTag", ExecTriggerStoryByTag);
            Register("AddToFatePool",     ExecAddToFatePool);
            Register("AddToKarmaPool",    ExecAddToKarmaPool);
            Register("EmitEvent",         ExecEmitEvent);
        }

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

        /// <summary>args[0]=npcId, args[1]=traitId</summary>
        private static void ExecGiveTrait(StoryContext ctx, List<string> args)
        {
            if (args.Count < 2) { LogMgr.Warn("[StoryEffectFunc] GiveTrait 参数不足"); return; }
            if (!int.TryParse(args[0], out int npcIdVal)) { LogMgr.Warn("[StoryEffectFunc] GiveTrait npcId 非整数"); return; }
            var npcId   = new NpcId(npcIdVal);
            var traitId = new TraitId(args[1]);
            NpcMgr.Instance?.Traits?.AddTrait(npcId, traitId);
            LogMgr.Dbg("[StoryEffectFunc] GiveTrait npc={0} trait={1}", npcIdVal, args[1]);
        }

        /// <summary>args[0]=npcId, args[1]=traitId</summary>
        private static void ExecRemoveTrait(StoryContext ctx, List<string> args)
        {
            if (args.Count < 2) { LogMgr.Warn("[StoryEffectFunc] RemoveTrait 参数不足"); return; }
            if (!int.TryParse(args[0], out int npcIdVal)) return;
            var npcId   = new NpcId(npcIdVal);
            var traitId = new TraitId(args[1]);
            NpcMgr.Instance?.Traits?.RemoveTrait(npcId, traitId);
            LogMgr.Dbg("[StoryEffectFunc] RemoveTrait npc={0} trait={1}", npcIdVal, args[1]);
        }

        /// <summary>args[0]=npcId, args[1]=cardDefineId</summary>
        private static void ExecGiveActionCard(StoryContext ctx, List<string> args)
        {
            if (args.Count < 2) { LogMgr.Warn("[StoryEffectFunc] GiveActionCard 参数不足"); return; }
            if (!int.TryParse(args[0], out int npcIdVal)) return;
            var npcId = new NpcId(npcIdVal);
            ActionCardMgr.Instance?.GiveCard(npcId, args[1]);
            LogMgr.Dbg("[StoryEffectFunc] GiveActionCard npc={0} card={1}", npcIdVal, args[1]);
        }

        /// <summary>args[0]=planeId(int), args[1]=element(string), args[2]=delta(float)</summary>
        private static void ExecModifyAura(StoryContext ctx, List<string> args)
        {
            if (args.Count < 3) { LogMgr.Warn("[StoryEffectFunc] ModifyAura 参数不足"); return; }
            // TODO: 待 PlaneMgr/TileSystemAura 提供直接修改接口后接入
            LogMgr.Dbg("[StoryEffectFunc] ModifyAura plane={0} element={1} delta={2}（暂未实现）", args[0], args[1], args[2]);
        }

        /// <summary>args[0]=npcId, args[1]=statId, args[2]=delta(float)</summary>
        private static void ExecModifyStat(StoryContext ctx, List<string> args)
        {
            if (args.Count < 3) { LogMgr.Warn("[StoryEffectFunc] ModifyStat 参数不足"); return; }
            if (!int.TryParse(args[0], out int npcIdVal)) return;
            if (!float.TryParse(args[2], out float delta)) return;
            var npc = NpcMgr.Instance?.GetById(new NpcId(npcIdVal));
            if (npc == null) { LogMgr.Warn("[StoryEffectFunc] ModifyStat 找不到 NPC {0}", npcIdVal); return; }
            npc.Stats.AddFlat(args[1], delta);
            LogMgr.Dbg("[StoryEffectFunc] ModifyStat npc={0} stat={1} delta={2}", npcIdVal, args[1], delta);
        }

        /// <summary>args[0]=storyId, args[1]=subjectId(npcId int, 可选)</summary>
        private static void ExecTriggerStory(StoryContext ctx, List<string> args)
        {
            if (args.Count < 1) { LogMgr.Warn("[StoryEffectFunc] TriggerStory 参数不足"); return; }
            var storyId = args[0];
            object? subject = ctx.Subject;
            if (args.Count >= 2 && int.TryParse(args[1], out int npcIdVal))
                subject = NpcMgr.Instance?.GetById(new NpcId(npcIdVal));
            StoryMgr.Instance?.TriggerStory(storyId, subject, ctx.SourcePool, ctx.Rng);
        }

        /// <summary>args[0..]=tags, 最后一个参数若可解析为 int 则视为 subjectId</summary>
        private static void ExecTriggerStoryByTag(StoryContext ctx, List<string> args)
        {
            if (args.Count < 1) { LogMgr.Warn("[StoryEffectFunc] TriggerStoryByTag 参数不足"); return; }
            StoryMgr.Instance?.TriggerStoryByTags(args, ctx.Subject, ctx.SourcePool, ctx.Rng);
        }

        /// <summary>args[0]=subjectId(npcId), args[1]=time(float), args[2]=storyId</summary>
        private static void ExecAddToFatePool(StoryContext ctx, List<string> args)
        {
            if (args.Count < 3) { LogMgr.Warn("[StoryEffectFunc] AddToFatePool 参数不足"); return; }
            if (!int.TryParse(args[0], out int npcIdVal)) return;
            if (!float.TryParse(args[1], out float time)) return;
            var npcId = new NpcId(npcIdVal);
            StoryMgr.Instance?.AddToFatePool(npcId, time, args[2]);
            LogMgr.Dbg("[StoryEffectFunc] AddToFatePool npc={0} time={1} story={2}", npcIdVal, time, args[2]);
        }

        /// <summary>args[0]=subjectId(npcId), args[1]=storyId, args[2]=weight(float)</summary>
        private static void ExecAddToKarmaPool(StoryContext ctx, List<string> args)
        {
            if (args.Count < 3) { LogMgr.Warn("[StoryEffectFunc] AddToKarmaPool 参数不足"); return; }
            if (!int.TryParse(args[0], out int npcIdVal)) return;
            if (!float.TryParse(args[2], out float weight)) return;
            var npcId = new NpcId(npcIdVal);
            StoryMgr.Instance?.AddToKarmaPool(npcId, args[1], weight);
            LogMgr.Dbg("[StoryEffectFunc] AddToKarmaPool npc={0} story={1} weight={2}", npcIdVal, args[1], weight);
        }

        /// <summary>args[0]=eventName, args[1..]=可选附加参数</summary>
        private static void ExecEmitEvent(StoryContext ctx, List<string> args)
        {
            if (args.Count < 1) { LogMgr.Warn("[StoryEffectFunc] EmitEvent 参数不足"); return; }
            EventMgr.Instance?.EmitEvent(args[0]);
            LogMgr.Dbg("[StoryEffectFunc] EmitEvent {0}", args[0]);
        }
    }
}
