using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 事件定义：描述一个游戏事件的元数据，包括事件 ID、显示名和声明要广播的 Scope 列表。
    /// 可由策划或玩家通过 JSON 扩展新事件，无需修改 C# 代码。
    /// </summary>
    public class EventDefine : DefineBase
    {
        /// <summary>
        /// 该事件声明要广播到的 Scope 列表。
        /// EventMgr 在触发时会对比调用方传入的 scope，缺失时输出 Warn。
        /// </summary>
        public EventScope[] Scopes { get; set; } = [];
    }
}
