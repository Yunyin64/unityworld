namespace UnityWorld.Core
{
    /// <summary>
    /// 可控随机数生成器(不使用UnityEngine.Random)
    /// </summary>
    public class Rng
    {
        static HashSet<int> _usedIds = new HashSet<int>();
        private readonly System.Random _random;

        public Rng(int seed)
        {
            _random = new System.Random(seed);
        }

        
        public Rng()
        {
            _random = new System.Random();
        }

        public float Range(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        public int Range(int min, int max)
        {
            return _random.Next(min, max);
        }
        
        

        /// <summary>生成 8 位随机整数 ID（10_000_000 ~ 99_999_999），用于运行时实体唯一标识</summary>
        public int NewId()
        {
            int id;
            do
            {
                id = _random.Next(10_000_000, 100_000_000);
            }
            while (_usedIds.Contains(id));
            _usedIds.Add(id);
            return id;
        }
    }
}