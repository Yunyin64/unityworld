using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 元气修正静态配置模板。
    /// 由策划在 JSON 中配置，运行时通过 <see cref="CreateModifier"/> 实例化为 <see cref="TileModifier"/>
    /// 挂载到地块上。
    ///
    /// <para><b>语义说明</b>：<see cref="AuraData"/> 是「目标浓度偏移量」，不是每秒速率。
    /// 它代表该修正源希望将地块元气向目标值推动的量，
    /// 实际变化速度由 <c>TileSystemAura</c> 的灵气变化速度常量控制。</para>
    /// </summary>
    public class TileModifierDefine : DefineBase
    {
        /// <summary>
        /// 目标浓度偏移量（各五行的累加量）。
        /// 非每秒速率，是对地块元气的固定目标偏移。
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
