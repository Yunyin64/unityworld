using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard
    {
        /// <summary>本卡实例的 Lua 脚本表。</summary>
        public LuaTable env { get; set; }
        /// <summary>本卡脚本声明的回调函数。</summary>
        public Dictionary<string, LuaFunction> LuaHooks { get; set; } = new();
        /// <summary>Lua 绑定的回调使用记录，每次 Tick 结束时清空。</summary>
        public HashSet<string> HookUse { get; set; } = new();

        /// <summary>
        /// 在 PreStart 加载本卡脚本、缓存回调并补全派生关键词。
        /// </summary>
        public void InitializeLuaCards()
        {
            var main = CardKeywordRoles.RequireMain(GetKeywords(), DefineId);
            // 加载 Lua 脚本，获得独立的 card table
            env = LuaMgr.Instance.LoadCardScript(CardDefineMgr.Instance.Get(DefineId));
            // 注入 C# 卡牌实例引用，供 Lua 通过  self.m_Self / self.m_Owner 访问
            if (env != null)
            {
                env["m_Self"] = this;
                env["m_Owner"] = this.Owner;
            }
            // 预扫描 Lua 函数缓存
            LuaHooks = LuaMgr.ScanLuaHooks(env);

            // 从 Lua CardData/Keywords 覆写 BaseData
            ApplyLuaOverrides();

            // 遍历所有 Keyword 的 CheckCondition，派生 keyword
            var derived = LuaMgr.Instance.RunKeywordConditions(this);
            foreach (var kw in derived)
            {
                if (!GetKeywords().Contains(kw))
                    GetKeywords().Add(kw);
            }
            // 旧卡脚本也能为战斗快照补上 Amount。同一实例再次入场保留已支付的余弹。
            if (_currentAmount < 0 && IsAmount) _currentAmount = GetAmountMax();
            // 装配和战斗共用 Main。普通 Lua 附加词仍可加入，新增第二个主词会明确报错。
            if (CardKeywordRoles.RequireMain(GetKeywords(), DefineId) != main)
                throw new InvalidOperationException($"卡牌 {DefineId} 的 Main Keyword 不能在 Lua 中变更。");
        }

        /// <summary>
        /// 将脚本的 Keywords 与旧 CardType 合入本场卡牌快照。
        /// </summary>
        private void ApplyLuaOverrides()
        {
            if (env == null) return;
            var baseData = BaseData;
            if (baseData == null) return;

            // ── 覆写 CardData，先不处理 TODO────────────────────────────────────
            /*
            if (env["CardData"] is LuaTable cardData)
            {
                if (cardData["Size"] is long size)
                    baseData.Size = (int)size;

                if (cardData["Cooldown"] is long cd)
                    baseData.Cooldown = cd;
                else if (cardData["Cooldown"] is double cdD)
                    baseData.Cooldown = (float)cdD;

                if (cardData["ManaCost"] is LuaTable manaTbl)
                {
                    baseData.ManaCost.Clear();
                    foreach (var key in manaTbl.Keys)
                    {
                        var elemStr = key.ToString();
                        if (Enum.TryParse<BaseElementType>(elemStr, out var baseElem))
                        {
                            var val = manaTbl[key];
                            int cost = val is long l ? (int)l : (int)(double)val;
                            baseData.ManaCost[new ElementType(baseElem)] = cost;
                        }
                    }
                }
            }

            */
            // ── 覆写 Keywords ────────────────────────────────────
            // nil 或空表不修改，保留 C# 侧已有 Keywords
            if (env["Keywords"] is LuaTable kwTbl && kwTbl.Values.Count > 0)
            {
                foreach (var val in kwTbl.Values)
                {
                    var kw = val.ToString();
                    if (!baseData.Keywords.Contains(kw))
                        baseData.Keywords.Add(kw);
                }
            }

            // CardData 里的 CardType 字段也视为 keyword（兼容旧写法）
            if (env["CardData"] is LuaTable cd2 && cd2["CardType"] is string cardType)
            {
                if (!string.IsNullOrEmpty(cardType) && !baseData.Keywords.Contains(cardType))
                    baseData.Keywords.Add(cardType);
            }
        }

        /// <summary>
        /// 创建当前卡牌的默认 APIContext。
        /// </summary>
        private APIContext CreateCtx() => new APIContext
        {
            SourceCard = this,
            Caster = Owner,
            Scene = Owner?.Scene
        };

        /// <summary>创建卡牌上下文并设置一个参数</summary>
        private APIContext CreateCtx<T>(string key, T value)
        {
            var api = CreateCtx();
            api.Set(key, value);
            return api;
        }

        /// <summary>先执行本卡回调，再将同一上下文交给关键词回调。</summary>
        public void CallLua(string hookName, APIContext eventCtx = null)
        {
            eventCtx ??= CreateCtx();
            string alias = hookName is "PreStart" or "Start" ? "On" + hookName : null;
            InvokeOwnHook(this.HasHook(hookName) ? hookName : alias ?? hookName, eventCtx);
            RunKeywordHooks(hookName, eventCtx, alias);
        }

        /// <summary>已登记销毁，等待 Tick 开头或战末清理。</summary>
        internal bool MarkedForDestruction { get; set; }

        /// <summary>获取主关键词生命周期表中的指定 Lua 钩子</summary>
        private LuaFunction GetMainKeywordHook(string hookName)
        {
            var name = BaseData.MainKeyword.ToString();
            var keyword = LuaMgr.Instance?.GetKeyword(name)
                ?? throw new InvalidOperationException($"卡牌 {DefineId} 的主关键词 {name} 未注册。");
            using var lifecycle = keyword["Lifecycle"] as LuaTable
                ?? throw new InvalidOperationException($"卡牌 {DefineId} 的主关键词 {name} 缺少 Lifecycle 定义。");
            return lifecycle[hookName] as LuaFunction;
        }

        // 直接调用本卡唯一主词，不走全场广播、卡脚本或普通关键词，也不记录额外表现。
        /// <summary>调用主关键词钩子并返回首个结果</summary>
        internal object CallMainKeyword(string hookName, APIContext ctx = null, object argument = null)
        {
            using var hook = GetMainKeywordHook(hookName);
            if (hook == null) return null;
            var result = hook.Call(this, ctx, argument);
            Scene?.ThrowIfEventAborted();
            return result.Length == 0 ? null : result[0];
        }

        /// <summary>主关键词提供了末格发动资格判定时，本卡可蓄灵并参与扫描发动。</summary>
        public bool HasScanOpportunity
        {
            get
            {
                using var hook = GetMainKeywordHook("CanCommitScan");
                return hook != null;
            }
        }

        /// <summary>按主关键词资格检查和链内去重后分发事件</summary>
        internal void DispatchCombatEvent(string hookName, APIContext ctx, HashSet<int> respondedCards)
        {
            if (CallMainKeyword("CanRespond", ctx, false) is not true) return;
            // 同一条事件链内，内容相同的事件只交给这张牌处理一次。
            if (!respondedCards.Add(Id)) return;
            CallLua(hookName, ctx);
        }

        /// <summary>执行本卡脚本的指定回调，供主关键词安排生命周期。</summary>
        public void InvokeOwnHook(string hookName, APIContext ctx)
        {
            using var presentation = Scene?.BeginPresentationEffect(this, hookName);
            this.CallLuaHook<bool>(hookName, env, ctx);
            Scene?.ThrowIfEventAborted();
            presentation?.Complete();
        }

        /// <summary>
        /// 遍历卡牌 Keywords 列表，查 LuaMgr 注册表调用对应 hook 函数。
        /// keyword 未注册 → 报错日志；无对应 hook 时尝试 fallback，两者都没有则静默跳过。
        /// </summary>
        public void RunKeywordHooks(string hookName, APIContext ctx, string fallback = null)
            => DispatchKeywordHooks(hookName, ctx, fallback);

        /// <summary>分发关键词回调，实际离场或战末可采纳销毁请求。</summary>
        internal void DispatchKeywordHooks(string hookName, APIContext ctx, string fallback = null, bool acceptDestruction = false)
        {
            var keywords = GetKeywords();
            if (keywords == null || keywords.Count == 0) return;

            var luaMgr = LuaMgr.Instance;
            if (luaMgr == null) return;

            foreach (var kw in keywords.Distinct().ToArray())
            {
                var kwTable = luaMgr.GetKeyword(kw);
                if (kwTable == null)
                {
                    Log($"Keyword '{kw}' 未注册");
                    continue;
                }

                var func = kwTable[hookName] as NLua.LuaFunction;
                if (func == null && fallback != null) func = kwTable[fallback] as NLua.LuaFunction;
                if (func == null) continue; // hook 不存在，静默跳过

                try
                {
                    using var presentation = Scene?.BeginPresentationEffect(this, "Keyword:" + kw + ":" + hookName);
                    var result = func.Call(this, ctx);
                    Scene?.ThrowIfEventAborted();
                    if (acceptDestruction && result.Length > 0 && result[0] is "DestroyCard")
                        MarkedForDestruction = true;
                    presentation?.Complete();
                }
                catch (Exception ex)
                {
                    Scene?.ThrowIfEventAborted();
                    Log($"Keyword '{kw}' hook '{hookName}' 异常: {ex.Message}");
                }
            }
        }
    }
}
