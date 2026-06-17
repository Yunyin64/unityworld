using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 大世界（Story）域的 API 函数实现集合。
    /// 从 StoryEffectFunc 迁移而来，每个方法以 [APIFunc] 标记，
    /// 由 APIMgr 反射扫描自动注册。
    /// </summary>
    public static class StoryBaseFunc
    {
        // ── Trait 操作 ────────────────────────────────────────

        /// <summary>给NPC添加特质。参数：int(String), TraitId(String)</summary>
        [APIFunc("GiveTrait", APIType.Action, "给NPC添加特质", Scope.Npc, "int:String", "TraitId:String")]
        public static void GiveTrait(ContextBase ctx)
        {
            string npcIdStr = ctx.GetValue("int", "");
            string traitId = ctx.GetValue("TraitId", "");
            if (!int.TryParse(npcIdStr, out int npcIdVal))
            {
                LogMgr.Instance.Warn("[StoryBaseFunc] GiveTrait npcId 非整数: {0}", npcIdStr);
                return;
            }
            NpcMgr.Instance?.TraitSystem?.AddTrait(NpcMgr.Instance.GetById(npcIdVal), new TraitId(traitId));
            LogMgr.Instance.Dbg("[StoryBaseFunc] GiveTrait npc={0} trait={1}", npcIdVal, traitId);
        }

        /// <summary>移除NPC特质。参数：int(String), TraitId(String)</summary>
        [APIFunc("RemoveTrait", APIType.Action, "移除NPC特质", Scope.Npc, "int:String", "TraitId:String")]
        public static void RemoveTrait(ContextBase ctx)
        {
            string npcIdStr = ctx.GetValue("int", "");
            string traitId = ctx.GetValue("TraitId", "");
            if (!int.TryParse(npcIdStr, out int npcIdVal)) return;
            NpcMgr.Instance?.TraitSystem?.RemoveTrait(NpcMgr.Instance.GetById(npcIdVal), new TraitId(traitId));
            LogMgr.Instance.Dbg("[StoryBaseFunc] RemoveTrait npc={0} trait={1}", npcIdVal, traitId);
        }

        // ── 行为卡操作 ────────────────────────────────────────

        /// <summary>给NPC添加行为卡。参数：int(String), CardDefineId(String)</summary>
        [APIFunc("GiveBehaviorCard", APIType.Action, "给NPC添加行为卡", Scope.Npc, "int:String", "CardDefineId:String")]
        public static void GiveBehaviorCard(ContextBase ctx)
        {
            string npcIdStr = ctx.GetValue("int", "");
            string cardDefineId = ctx.GetValue("CardDefineId", "");
            if (!int.TryParse(npcIdStr, out int npcIdVal)) return;
            BehaviorCardMgr.Instance?.GiveCard(npcIdVal, cardDefineId);
            LogMgr.Instance.Dbg("[StoryBaseFunc] GiveBehaviorCard npc={0} card={1}", npcIdVal, cardDefineId);
        }

        // ── 灵气/属性修改 ─────────────────────────────────────

        /// <summary>修改地块五行浓度。参数：PlaneId(String), Element(String), Delta(Float)</summary>
        [APIFunc("ModifyAura", APIType.Action, "修改地块五行浓度", Scope.Global, "PlaneId:String", "Element:String", "Delta:Float")]
        public static void ModifyAura(ContextBase ctx)
        {
            string planeId = ctx.GetValue("PlaneId", "");
            string element = ctx.GetValue("Element", "");
            float delta = ctx.GetValue("Delta", 0f);
            // TODO: 待 PlaneMgr/TileSystemAura 提供直接修改接口后接入
            LogMgr.Instance.Dbg("[StoryBaseFunc] ModifyAura plane={0} element={1} delta={2}（暂未实现）", planeId, element, delta);
        }

        /// <summary>修改NPC属性值。参数：int(String), StatId(String), Delta(Float)</summary>
        [APIFunc("ModifyStat", APIType.Action, "修改NPC属性值", Scope.Npc, "int:String", "StatId:String", "Delta:Float")]
        public static void ModifyStat(ContextBase ctx)
        {
            string npcIdStr = ctx.GetValue("int", "");
            string statId = ctx.GetValue("StatId", "");
            float delta = ctx.GetValue("Delta", 0f);
            if (!int.TryParse(npcIdStr, out int npcIdVal)) return;
            var npc = NpcMgr.Instance?.GetById(npcIdVal);
            if (npc == null)
            {
                LogMgr.Instance.Warn("[StoryBaseFunc] ModifyStat 找不到 NPC {0}", npcIdVal);
                return;
            }
            // TODO: 接入 StatBlock 的 AddFlat
            LogMgr.Instance.Dbg("[StoryBaseFunc] ModifyStat npc={0} stat={1} delta={2}", npcIdVal, statId, delta);
        }

        // ── Story 触发 ───────────────────────────────────────

        /// <summary>链式触发Story。参数：StoryId(String), SubjectId(String)</summary>
        [APIFunc("TriggerStory", APIType.Action, "链式触发Story", Scope.Global, "StoryId:String", "SubjectId:String")]
        public static void TriggerStory(ContextBase ctx)
        {
            string storyId = ctx.GetValue("StoryId", "");
            string subjectIdStr = ctx.GetValue("SubjectId", "");

            object subject = ctx.Get<object>("Subject");
            if (!string.IsNullOrEmpty(subjectIdStr) && int.TryParse(subjectIdStr, out int npcIdVal))
            {
                subject = NpcMgr.Instance?.GetById(npcIdVal);
            }

            var sourcePool = ctx.GetValue<StoryPoolSource>("SourcePool");
            StoryMgr.Instance?.TriggerStory(storyId, subject, sourcePool);
        }

        /// <summary>按Tag匹配触发Story。参数：Tags(String)（逗号分隔）</summary>
        [APIFunc("TriggerStoryByTag", APIType.Action, "按Tag匹配触发Story", Scope.Global, "Tags:String")]
        public static void TriggerStoryByTag(ContextBase ctx)
        {
            string tagsStr = ctx.GetValue("Tags", "");
            var tags = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tags.Count == 0)
            {
                LogMgr.Instance.Warn("[StoryBaseFunc] TriggerStoryByTag 参数 Tags 为空");
                return;
            }

            var subject = ctx.Get<object>("Subject");
            var sourcePool = ctx.GetValue<StoryPoolSource>("SourcePool");
            StoryMgr.Instance?.TriggerStoryByTags(tags, subject, sourcePool);
        }

        // ── 池操作 ───────────────────────────────────────────

        /// <summary>向宿命池写入条目。参数：SubjectId(String), Time(Float), StoryId(String)</summary>
        [APIFunc("AddToFatePool", APIType.Action, "向宿命池写入条目", Scope.Npc, "SubjectId:String", "Time:Float", "StoryId:String")]
        public static void AddToFatePool(ContextBase ctx)
        {
            string subjectIdStr = ctx.GetValue("SubjectId", "");
            float time = ctx.GetValue("Time", 0f);
            string storyId = ctx.GetValue("StoryId", "");
            if (!int.TryParse(subjectIdStr, out int npcIdVal)) return;
            StoryMgr.Instance?.AddToFatePool(npcIdVal, time, storyId);
            LogMgr.Instance.Dbg("[StoryBaseFunc] AddToFatePool npc={0} time={1} story={2}", npcIdVal, time, storyId);
        }

        /// <summary>向劫缘池写入条目。参数：SubjectId(String), StoryId(String), Weight(Float)</summary>
        [APIFunc("AddToKarmaPool", APIType.Action, "向劫缘池写入条目", Scope.Npc, "SubjectId:String", "StoryId:String", "Weight:Float")]
        public static void AddToKarmaPool(ContextBase ctx)
        {
            string subjectIdStr = ctx.GetValue("SubjectId", "");
            string storyId = ctx.GetValue("StoryId", "");
            float weight = ctx.GetValue("Weight", 0f);
            if (!int.TryParse(subjectIdStr, out int npcIdVal)) return;
            StoryMgr.Instance?.AddToKarmaPool(npcIdVal, storyId, weight);
            LogMgr.Instance.Dbg("[StoryBaseFunc] AddToKarmaPool npc={0} story={1} weight={2}", npcIdVal, storyId, weight);
        }

        // ── 事件广播 ─────────────────────────────────────────

        /// <summary>通过EventMgr广播事件。参数：EventName(String)</summary>
        [APIFunc("TriggerEvent", APIType.Action, "通过EventMgr广播事件", Scope.Global, "EventName:String")]
        public static void TriggerEvent(ContextBase ctx)
        {
            string eventName = ctx.GetValue("EventName", "");
            if (string.IsNullOrEmpty(eventName))
            {
                LogMgr.Instance.Warn("[StoryBaseFunc] TriggerEvent 参数 EventName 为空");
                return;
            }
            EventMgr.Instance?.TriggerEvent(eventName, "");
            LogMgr.Instance.Dbg("[StoryBaseFunc] TriggerEvent {0}", eventName);
        }
    }
}