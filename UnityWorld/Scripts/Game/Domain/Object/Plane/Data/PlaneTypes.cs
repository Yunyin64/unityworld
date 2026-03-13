namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面相关枚举类型
    /// </summary>
    public static class PlaneTypes
    {
        /// <summary>
        /// 位面种类
        /// </summary>
        public enum PlaneKind
        {
            /// <summary>主世界（永久存在，世界的主干�?/summary>
            MainPlane = 0,

            /// <summary>小世界（动态创�?销毁，由功法或事件驱动�?/summary>
            SubPlane = 1,
        }

        /// <summary>
        /// 位面当前状�?
        /// </summary>
        public enum PlaneState
        {
            /// <summary>活跃中（正常运行�?/summary>
            Active = 0,

            /// <summary>已封印（暂停Tick，但数据保留�?/summary>
            Sealed = 1,

            /// <summary>已销毁（待GC回收�?/summary>
            Destroyed = 2,
        }
    }
}
