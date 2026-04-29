using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
        // ── BirthContext ─────────────────────────────────
        /// <summary>
        /// NPC 诞生上下文：携带创建过程中各子系统需要的信息。
        /// 继承自 ContextBase，可通过 Set/Get/GetValue 传递任意键值。
        /// </summary>
        public class BirthContext : ContextBase
        {
                public Npc MainNpc;
        }
}