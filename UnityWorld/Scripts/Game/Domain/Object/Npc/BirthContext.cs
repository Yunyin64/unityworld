using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
        public enum NpcAssemblySource
        {
            /// <summary>随机生成角色来源</summary>
            Generated,
            /// <summary>玩家角色来源</summary>
            Player,
            /// <summary>配置模板装配来源</summary>
            Template
        }
        // ── BirthContext ─────────────────────────────────
        /// <summary>
        /// NPC 诞生上下文：携带创建过程中各子系统需要的信息。
        /// 继承自 ContextBase，可通过 Set/Get/GetValue 传递任意键值。
        /// </summary>
        public class BirthContext : ContextBase
        {
                /// <summary>正在创建的角色</summary>
                public Npc MainNpc;
                /// <summary>本次角色组装来源</summary>
                public NpcAssemblySource AssemblySource { get; set; } = NpcAssemblySource.Generated;
        }
}
