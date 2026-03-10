namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 强类型NPC ID
    /// </summary>
    public struct NpcId
    {
        public readonly int Value;

        public NpcId(int value)
        {
            Value = value;
        }

        public static readonly NpcId Invalid = new NpcId(-1);
    }

    /// <summary>
    /// NPC ID生成�?    /// </summary>
    public static class NpcIdGenerator
    {
        private static int _nextId = 1;

        public static NpcId Generate()
        {
            return new NpcId(_nextId++);
        }
    }
}