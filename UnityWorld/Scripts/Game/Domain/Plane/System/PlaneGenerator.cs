namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面地块生成器：根据位面的 Width × Height 填充初始地块。
    /// 坐标使用「奇数行右移（odd-r）」Offset 布局，生成后转换为 Axial 存入 Plane。
    /// </summary>
    public static class PlaneGenerator
    {
        /// <summary>
        /// 为指定位面填充所有地块（Width × Height 格）。
        /// 若位面 Width 或 Height 为 0，则跳过生成（空位面）。
        /// 已存在的地块不会被覆盖（支持存档恢复后补全）。
        /// </summary>
        /// <param name="plane">目标位面</param>
        public static void Fill(Plane plane)
        {
            if (plane.Width <= 0 || plane.Height <= 0) return;

            for (int row = 0; row < plane.Height; row++)
            {
                for (int col = 0; col < plane.Width; col++)
                {
                    var axial = Plane.OffsetToAxial(col, row);
                    if (!plane.HasTile(axial))
                        plane.SetTile(new Tile(axial));
                }
            }
        }
    }
}
