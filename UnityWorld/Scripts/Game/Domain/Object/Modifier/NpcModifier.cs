using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 修正源占位类，暂无额外字段，待 NPC 个体层迭代时补充。
    /// </summary>
    public class NpcModifier : IModifierBase,IFormDefine<NpcModifierDefine>,ILuaBindable
    {
        /// <summary>角色状态实例编号</summary>
        public string Id { get  ; set  ; }
        /// <summary>角色状态配置编号</summary>
        public string DefineId { get  ; set  ; }
        /// <summary>状态显示名</summary>
        public string DisplayName { get  ; set  ; }
        /// <summary>状态施加来源编号</summary>
        public string SourceId { get  ; set  ; }
        /// <summary>配置的持续时间</summary>
        public float Duration { get  ; set  ; }
        /// <summary>状态剩余持续时间</summary>
        public float RemainingTime { get  ; set  ; }
        /// <summary>状态提供的属性修正</summary>
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        /// <summary>状态脚本的 Lua 环境</summary>
        public LuaTable env { get  ; set  ; }
        /// <summary>按名称缓存的 Lua 钩子</summary>
        public Dictionary<string, LuaFunction> LuaHooks { get ; set ; } = new();
        /// <summary>已调用的 Lua 钩子名</summary>
        public HashSet<string> HookUse { get ; set ; } = new();
        /// <summary>配置的层数上限</summary>
        public int MaxStack { get  ; set  ; }
        /// <summary>当前状态层数</summary>
        public int CurrentStack { get  ; set  ; }
        /// <summary>叠加时是否重置持续时间</summary>
        public bool RefreshOnStack { get  ; set  ; }
        /// <summary>状态过期方式</summary>
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        /// <summary>触发移除的事件编号</summary>
        public string RemoveTriggerId { get  ; set  ; }
    }
}
