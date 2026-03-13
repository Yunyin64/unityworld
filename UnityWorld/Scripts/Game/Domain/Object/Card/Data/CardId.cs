namespace UnityWorld.Game.Domain.Card
{
    /// <summary>
    /// Card 强类型 ID
    /// </summary>
    public readonly struct CardId
    {
        public readonly int Value;

        public CardId(int value)
        {
            Value = value;
        }

        public static readonly CardId Invalid = new CardId(-1);
    }

    /// <summary>
    /// Card ID 生成器
    /// </summary>
    public static class CardIdGenerator
    {
        private static int _nextId = 1;

        public static CardId Generate() => new CardId(_nextId++);
    }
}
