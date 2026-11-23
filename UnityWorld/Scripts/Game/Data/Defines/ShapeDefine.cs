using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>从卡形外接矩形左上角起算的相对占格。</summary>
    public sealed class ShapeDefine : DefineBase
    {
        /// <summary>形状实际占用的格子，坐标从零起。</summary>
        public List<BackpackCell> Cells { get; set; } = [];
    }
}
