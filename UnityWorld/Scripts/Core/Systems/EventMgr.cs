using System;
using System.Collections;
using System.Collections.Generic;
using UnityWorld.Game.Data;

namespace UnityWorld.Core
{
    /// <summary>
    /// 事件管理器：支持 Scope 分层的高性能事件广播总线。
    /// 使用字符串 eventId 替代枚举，支持策划/玩家通过 EventDefine JSON 扩展新事件。
    /// 支持 Global / Npc / Tile / Plane 等 Scope 层级的精准广播。
    /// </summary>
    public class EventMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────

        /// <summary>全局单例，使用前请做空检查</summary>
        public static EventMgr? Instance { get; private set; }

        /// <inheritdoc/>
        public string Name => "EventMgr";

        /// <inheritdoc/>
        public string Desc => "Scope 分层事件广播总线，支持 EventDefine JSON 扩展";

        // ── 内部类型 ──────────────────────────────────────────

        /// <summary>监听者条目，包含调试 Key 和监听者实例</summary>
        private sealed class ListenerEntry
        {
            public string         ListenerKey { get; }
            public IEventListener Listener    { get; }

            public ListenerEntry(string listenerKey, IEventListener listener)
            {
                ListenerKey = listenerKey;
                Listener    = listener;
            }
        }

        // ── 延迟操作（触发中保护） ────────────────────────────

        private readonly struct PendingOp
        {
            public enum OpType { Add, Remove }
            public OpType         Type        { get; }
            public string         EventId     { get; }
            public ScopeKey       Scope       { get; }
            public string         ListenerKey { get; }
            public IEventListener Listener    { get; }

            public PendingOp(OpType type, string eventId, ScopeKey scope, string listenerKey, IEventListener listener)
            {
                Type        = type;
                EventId     = eventId;
                Scope       = scope;
                ListenerKey = listenerKey;
                Listener    = listener;
            }
        }

        // ── 核心存储 ──────────────────────────────────────────

        /// <summary>主监听表：eventId → (ScopeKey → 监听者列表)</summary>
        private readonly Dictionary<string, Dictionary<ScopeKey, List<ListenerEntry>>> _listeners
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>触发中时的延迟操作队列</summary>
        private readonly List<PendingOp> _pendingOps = new();

        /// <summary>触发嵌套深度计数器（防止触发中修改集合）</summary>
        private int _triggering = 0;

        // ── 生命周期 ──────────────────────────────────────────

        /// <inheritdoc/>
        public void Init()
        {
            Instance = this;
        }

        /// <inheritdoc/>
        public void Begin() { }

        /// <inheritdoc/>
        public void Tick(float deltaTime) { }

        /// <inheritdoc/>
        public void Update() { }

        /// <inheritdoc/>
        public void Render(float dt) { }

        /// <inheritdoc/>
        public void End()
        {
            _listeners.Clear();
            _pendingOps.Clear();
            Instance = null;
        }

        /// <inheritdoc/>
        public IEnumerator Save() { yield break; }

        /// <inheritdoc/>
        public IEnumerator Load() { yield break; }

        // ── 注册 / 移除 ───────────────────────────────────────

        /// <summary>
        /// 注册事件监听者到指定 eventId 和 scope。
        /// </summary>
        /// <param name="listenerKey">调试标识 Key，用于排查悬空监听时定位来源</param>
        /// <param name="eventId">事件字符串 ID，如 "NpcMoved"</param>
        /// <param name="scope">监听的 Scope 实例（如 <c>new ScopeKey(EventScope.Npc, npcId)</c>）</param>
        /// <param name="listener">监听者实例</param>
        public void RegisterEvent(string listenerKey, string eventId, ScopeKey scope, IEventListener listener)
        {
            if (_triggering > 0)
            {
                _pendingOps.Add(new PendingOp(PendingOp.OpType.Add, eventId, scope, listenerKey, listener));
                return;
            }
            DoRegister(listenerKey, eventId, scope, listener);
        }

        /// <summary>
        /// 移除已注册的事件监听者。若监听者不存在则静默忽略。
        /// 在触发中调用时，延迟到本次触发结束后执行。
        /// </summary>
        /// <param name="eventId">事件字符串 ID</param>
        /// <param name="scope">已注册时使用的 ScopeKey</param>
        /// <param name="listener">要移除的监听者实例</param>
        public void RemoveEvent(string eventId, ScopeKey scope, IEventListener listener)
        {
            if (_triggering > 0)
            {
                _pendingOps.Add(new PendingOp(PendingOp.OpType.Remove, eventId, scope, string.Empty, listener));
                return;
            }
            DoRemove(eventId, scope, listener);
        }

        // ── 触发 ──────────────────────────────────────────────

        /// <summary>
        /// 触发事件，向 Global scope 和所有传入 scope 广播。
        /// 若 EventDefineMgr 中存在该事件定义，会校验调用方传入的 scope 是否覆盖定义中声明的 scope，
        /// 缺失时输出 <see cref="LogMgr.Warn"/>。
        /// </summary>
        /// <param name="eventId">事件字符串 ID，如 "NpcMoved"</param>
        /// <param name="args">事件参数（由事件定义约定具体类型）</param>
        /// <param name="scopes">涉及的 scope 实例列表（自动补充 Global，无需显式传入）</param>
        public void TriggerEvent(string eventId, object args, params (EventScope scope, string id)[] scopes)
        {
            // ① 校验事件是否在 EventDefine 中已定义
            var define = EventDefineMgr.Instance?.Get(eventId);
            if (define == null && EventDefineMgr.Instance != null)
            {
                LogMgr.Warn("[EventMgr] 触发了未在 EventDefine 中定义的事件：{0}，请检查 EventDefines.json", eventId);
            }

            // ② 校验调用方传入的 scope 是否覆盖 EventDefine 声明的 scope
            if (define != null && define.Scopes.Length > 0)
            {
                foreach (var declaredScope in define.Scopes)
                {
                    if (declaredScope == EventScope.Global) continue; // Global 自动补充，不需要检查
                    bool found = false;
                    foreach (var (s, _) in scopes)
                    {
                        if (s == declaredScope) { found = true; break; }
                    }
                    if (!found)
                    {
                        LogMgr.Warn("[EventMgr] 事件 {0}：EventDefine 声明了 {1} scope，但调用方未提供对应 ID", eventId, declaredScope);
                    }
                }
            }

            _triggering++;
            try
            {
                // ③ 广播 Global scope（自动补充）
                DispatchToScope(eventId, ScopeKey.Global, args);

                // ④ 广播调用方传入的各 scope
                foreach (var (scopeType, id) in scopes)
                {
                    if (scopeType == EventScope.Global) continue; // 已在上面处理
                    DispatchToScope(eventId, new ScopeKey(scopeType, id), args);
                }
            }
            catch (Exception ex)
            {
                LogMgr.Err("[EventMgr] TriggerEvent({0}) 发生异常：{1}", eventId, ex);
            }
            finally
            {
                _triggering--;
                if (_triggering <= 0) FlushPendingOps();
            }
        }

        // ── 调试查询 ──────────────────────────────────────────

        /// <summary>
        /// 获取指定 eventId + scope 下所有监听者的 Key 列表（用于调试）。
        /// </summary>
        public IEnumerable<string> GetListenerKeys(string eventId, ScopeKey scope)
        {
            if (_listeners.TryGetValue(eventId, out var scopeMap) &&
                scopeMap.TryGetValue(scope, out var entries))
            {
                foreach (var e in entries) yield return e.ListenerKey;
            }
        }

        // ── 旧接口兼容（Lua 残留，待 Lua 集成时替换） ─────────

        /// <summary>
        /// 旧版 Lua 事件注册标记接口，已废弃。
        /// </summary>
        [Obsolete("LuaRegisterEvent 已废弃，请使用 RegisterEvent 配合 LuaEventListener")]
        public void LuaRegisterEvent(Em_Event e) { }

        /// <summary>
        /// 旧版 Lua 事件注销标记接口，已废弃。
        /// </summary>
        [Obsolete("LuaUnRegisterEvent 已废弃，请使用 RemoveEvent 配合 LuaEventListener")]
        public void LuaUnRegisterEvent(Em_Event e) { }

        /// <summary>
        /// 旧版 Step 方法，已废弃，请使用 Tick。
        /// </summary>
        [Obsolete("Step 已废弃，请使用 Tick(float deltaTime)")]
        public void Step(float dt) { }

        // ── 私有实现 ──────────────────────────────────────────

        private void DoRegister(string listenerKey, string eventId, ScopeKey scope, IEventListener listener)
        {
            if (!_listeners.TryGetValue(eventId, out var scopeMap))
            {
                scopeMap = new Dictionary<ScopeKey, List<ListenerEntry>>();
                _listeners[eventId] = scopeMap;
            }
            if (!scopeMap.TryGetValue(scope, out var entries))
            {
                entries = new List<ListenerEntry>();
                scopeMap[scope] = entries;
            }
            // 幂等：同一 listener 实例不重复注册
            foreach (var e in entries)
            {
                if (ReferenceEquals(e.Listener, listener)) return;
            }
            entries.Add(new ListenerEntry(listenerKey, listener));
        }

        private void DoRemove(string eventId, ScopeKey scope, IEventListener listener)
        {
            if (!_listeners.TryGetValue(eventId, out var scopeMap)) return;
            if (!scopeMap.TryGetValue(scope, out var entries)) return;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(entries[i].Listener, listener))
                {
                    entries.RemoveAt(i);
                    return;
                }
            }
        }

        private void DispatchToScope(string eventId, ScopeKey scope, object args)
        {
            if (!_listeners.TryGetValue(eventId, out var scopeMap)) return;
            if (!scopeMap.TryGetValue(scope, out var entries) || entries.Count == 0) return;

            List<int>? staleIndices = null;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Listener == null)
                {
                    // 悬空监听：自动清理
                    staleIndices ??= new List<int>();
                    staleIndices.Add(i);
                    LogMgr.Warn("[EventMgr] 发现悬空监听，已自动移除。eventId={0} scope={1} listenerKey={2}",
                        eventId, scope, entry.ListenerKey);
                    continue;
                }
                try
                {
                    entry.Listener.OnEvent(eventId, scope, args);
                }
                catch (Exception ex)
                {
                    LogMgr.Err("[EventMgr] 监听者 {0} 处理事件 {1} 时抛出异常：{2}",
                        entry.ListenerKey, eventId, ex);
                }
            }

            // 清理悬空条目（从后往前删，不影响索引）
            if (staleIndices != null)
            {
                for (int i = staleIndices.Count - 1; i >= 0; i--)
                    entries.RemoveAt(staleIndices[i]);
            }
        }

        private void FlushPendingOps()
        {
            foreach (var op in _pendingOps)
            {
                if (op.Type == PendingOp.OpType.Add)
                    DoRegister(op.ListenerKey, op.EventId, op.Scope, op.Listener);
                else
                    DoRemove(op.EventId, op.Scope, op.Listener);
            }
            _pendingOps.Clear();
        }
    }
}