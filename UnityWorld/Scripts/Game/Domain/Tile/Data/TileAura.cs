namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块五行元气浓度（金木水火土各自独立数值，范围 0~∞，基准值为 1.0）
    /// </summary>
    public class TileAura
    {
        /// <summary>金元气浓度</summary>
        public float Jing { get; set; } = 1f;

        /// <summary>木元气浓度</summary>
        public float Mu { get; set; } = 1f;

        /// <summary>水元气浓度</summary>
        public float Shui { get; set; } = 1f;

        /// <summary>火元气浓度</summary>
        public float Huo { get; set; } = 1f;

        /// <summary>土元气浓度</summary>
        public float Tu { get; set; } = 1f;

        /// <summary>总元气浓度（五行之和）</summary>
        public float Total => Jing + Mu + Shui + Huo + Tu;

        /// <summary>获取指定五行的浓度</summary>
        public float Get(BaseElementType element) => element switch
        {
            BaseElementType.Jing  => Jing,
            BaseElementType.Mu    => Mu,
            BaseElementType.Shui  => Shui,
            BaseElementType.Huo   => Huo,
            BaseElementType.Tu    => Tu,
            _ => 0f,
        };

        /// <summary>设置指定五行的浓度</summary>
        public void Set(BaseElementType element, float value)
        {
            switch (element)
            {
                case BaseElementType.Jing: Jing = value; break;
                case BaseElementType.Mu:   Mu   = value; break;
                case BaseElementType.Shui: Shui = value; break;
                case BaseElementType.Huo:  Huo  = value; break;
                case BaseElementType.Tu:   Tu   = value; break;
            }
        }

        /// <summary>将另一个元气的值叠加到本元气（用于位面加成）</summary>
        public void AddFrom(TileAura bonus)
        {
            Jing += bonus.Jing;
            Mu   += bonus.Mu;
            Shui += bonus.Shui;
            Huo  += bonus.Huo;
            Tu   += bonus.Tu;
        }

        public override string ToString()
            => string.Format("Aura(金{0:F1} 木{1:F1} 水{2:F1} 火{3:F1} 土{4:F1})", Jing, Mu, Shui, Huo, Tu);
    }

}