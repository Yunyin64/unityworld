using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 元气修正静态配置模板。
    /// 由策划在 JSON 中配置，运行时通过 <see cref="CreateModifier"/> 实例化为 <see cref="TileModifier"/>
    /// 挂载到地块上。
    ///
    /// <para><b>语义说明</b>：<see cref="AuraData"/> 是每秒元气变化率。
    /// <c>TileSystemAura</c> 按传入的 deltaTime 累积，不作为地图生成初值。</para>
    /// </summary>
    public class TileModifierDefine : DefineBase
    {
        /// <summary>
        /// 每秒五行元气变化量，可正可负；保留的附加修正，不替代地标 AuraOffset 的阶段规则。
        /// </summary>
        public TileAura AuraData { get; set; } = new();

        /// <summary>
        /// 持续时间（秒）。
        /// -1 = 永久有效；大于 0 = 有限时长，到期后 Modifier 自动移除。
        /// </summary>
        public float Duration { get; set; } = -1f;

        // ── 工厂方法 ──────────────────────────────────────

        /// <summary>
        /// 从本定义实例化一个 <see cref="TileModifier"/>。
        /// </summary>
        /// <param name="sourceId">修正源标识（如地标实例 ID、NPC ID 等）</param>
        public TileModifier CreateModifier(string sourceId)
            => new TileModifier(ID, sourceId, AuraData, Duration);
    }
}
