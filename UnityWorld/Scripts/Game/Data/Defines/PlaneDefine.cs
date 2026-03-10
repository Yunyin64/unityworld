using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 位面定义/配置模板（由策划配置，用于批量创建同类位面）。
    /// 模板描述位面的静态属性，运行时由 PlaneMgr.CreatePlane(define) 实例化。
    /// </summary>
    public class PlaneDefine:DefineBase
    {

        /// <summary>位面类型</summary>
        public PlaneTypes.PlaneKind Kind { get; set; } = PlaneTypes.PlaneKind.SubPlane;

        /// <summary>
        /// 地图宽度（列数，即横向地块数）。
        /// 主世界必须显式指定；小世界默认 20。
        /// </summary>
        public int Width { get; set; } = 0;

        /// <summary>
        /// 地图高度（行数，即纵向地块数）。
        /// 主世界必须显式指定；小世界默认 20。
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>时间流速倍率（相对于世界主时间，默认 1.0）</summary>
        public float TimeFlowRate { get; set; } = 1f;

        /// <summary>位面级五行元气加成（叠加到每块地块上，默认全 0）</summary>
        public TileAura? AuraBonus { get; set; } = null;

        // ── 校验 ─────────────────────────────────────────

        /// <summary>
        /// 校验定义是否合法，不合法时抛出异常。
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ID))
                throw new InvalidOperationException($"[PlaneDefine] DefineId 不能为空");

            if (Kind == PlaneTypes.PlaneKind.MainPlane)
            {
                // 主世界：Width/Height 必须显式填写
                if (Width  <= 0) throw new InvalidOperationException(
                    $"[PlaneDefine:{ID}] 主世界 Width 必须 > 0（当前：{Width}）");
                if (Height <= 0) throw new InvalidOperationException(
                    $"[PlaneDefine:{ID}] 主世界 Height 必须 > 0（当前：{Height}）");
            }
            else
            {
                // 小世界：未填则补默认值 20×20
                if (Width  <= 0) Width  = 20;
                if (Height <= 0) Height = 20;
            }
        }

        /// <summary>
        /// 将本定义转换为 <see cref="PlaneConfig"/> 供 PlaneMgr 使用。
        /// 调用前会先执行 <see cref="Validate"/>。
        /// </summary>
        public PlaneConfig ToConfig()
        {
            Validate();
            return new PlaneConfig
            {
                Name         = DisplayName,
                Kind         = Kind,
                Width        = Width,
                Height       = Height,
                TimeFlowRate = TimeFlowRate,
                AuraBonus    = AuraBonus,
            };
        }

        // ── 内置工厂（开发期使用，未来可从 JSON 加载替代）────────

        /// <summary>默认主世界（100×60）</summary>
        public static PlaneDefine MainWorld(int width = 100, int height = 60) => new()
        {
            ID     = "main_world",
            DisplayName         = "主世界",
            Kind         = PlaneTypes.PlaneKind.MainPlane,
            Width        = width,
            Height       = height,
            TimeFlowRate = 1f,
        };

        /// <summary>通用小世界（默认 20×20）</summary>
        public static PlaneDefine SubPlane(string defineId, string name,
                                           int width = 20, int height = 20,
                                           float timeFlowRate = 1f,
                                           TileAura? auraBonus = null) => new()
        {
            ID     = defineId,
            DisplayName         = name,
            Kind         = PlaneTypes.PlaneKind.SubPlane,
            Width        = width,
            Height       = height,
            TimeFlowRate = timeFlowRate,
            AuraBonus    = auraBonus,
        };
    }
}
