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
        public string Id { get  ; set  ; }
        public string DefineId { get  ; set  ; }
        public string DisplayName { get  ; set  ; }
        public string SourceId { get  ; set  ; }
        public float Duration { get  ; set  ; }
        public float RemainingTime { get  ; set  ; }
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        public LuaTable env { get  ; set  ; }
        public Dictionary<string, LuaFunction> LuaHooks { get ; set ; } = new();
        public HashSet<string> HookUse { get ; set ; } = new();
        public int MaxStack { get  ; set  ; }
        public int CurrentStack { get  ; set  ; }
        public bool RefreshOnStack { get  ; set  ; }
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        public string RemoveTriggerId { get  ; set  ; }
    }
}
