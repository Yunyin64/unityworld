using System.Collections.Generic;
using UnityWorld.Game.Data;
using UnityWorld.Core;
using NLua;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 战斗修正源：战斗场景中 CombatNpc 的临时修正，按战斗 Tick 衰减。
    /// 四维结构：A 生命周期 / B 数值修正 / C 触发机制 / D 特殊实现
    /// </summary>
    public class CombatNpcModifier : IModifierBase,IFormDefine<CombatNpcModifierDefine>,ILuaBindable
    {
        public string Id { get  ; set  ; }
        public string DefineId { get  ; set  ; }
        public string DisplayName { get ; set  ; }
        public string SourceId { get  ; set  ; }
        public float Duration { get  ; set  ; }
        public float RemainingTime { get  ; set  ; }
        public LuaTable env { get  ; set  ; }

        // ── C. 触发机制 ─────────────────────────────────────

        /// <summary>触发器定义 ID 列表（复用卡牌 TCA 体系，注册到 EventMgr）</summary>
        public List<string> TriggerIds { get; set; } = new();

        // ── D. 特殊实现 ─────────────────────────────────────

        /// <summary>特殊实现标识（外部注册处理器查找键，如 "burn"、"transform"）</summary>
        public string ImplId { get; set; } = "";
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        public int MaxStack { get  ; set  ; }
        public int CurrentStack { get  ; set  ; }
        public bool RefreshOnStack { get  ; set  ; }

        
        // ── 工厂方法 ──────────────────────────────────────────

        /// <summary>
        /// 从本定义实例化一个 <see cref="CombatNpcModifier"/>。
        /// </summary>
        public static CombatNpcModifier CreateModifier(CombatNpcModifierDefine source)
            => new CombatNpcModifier()
            {
                DefineId = source.ID,
                DisplayName = source.DisplayName,
                Duration = source.Duration,
                RemainingTime = source.Duration,
                MaxStack = source.MaxStack,
                CurrentStack = 1,
                RefreshOnStack = source.RefreshOnStack,
                StatModifiers = source.StatModifiers,
                TriggerIds = source.TriggerIds.ToList(),
                ImplId = source.ImplId,
            };
    }


}
