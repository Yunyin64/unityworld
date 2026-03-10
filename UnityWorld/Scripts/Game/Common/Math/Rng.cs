namespace UnityWorld.Game.Common.Math
{
    /// <summary>
    /// 可控随机数生成器(不使用UnityEngine.Random)
    /// </summary>
    public class Rng
    {
        private readonly System.Random _random;

        public Rng(int seed)
        {
            _random = new System.Random(seed);
        }

        public float Range(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        public int Range(int min, int max)
        {
            return _random.Next(min, max);
        }
    }
}