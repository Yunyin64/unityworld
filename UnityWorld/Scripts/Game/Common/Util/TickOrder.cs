namespace UnityWorld.Game.Common.Util
{
    /// <summary>
    /// 集中定义系统tick顺序
    /// </summary>
    public static class TickOrder
    {
        public const int EarlySystems = 1000;
        public const int NpcSystems = 2000;
        public const int LateSystems = 3000;
    }
}