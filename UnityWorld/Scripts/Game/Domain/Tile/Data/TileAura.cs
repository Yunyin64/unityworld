namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块五行元气浓度（金木水火土各自独立数值，范围 0~∞，基准值为 1.0�?
    /// </summary>
    public class TileAura
    {
        /// <summary>金元气浓�?/summary>
        public float Metal { get; set; } = 1f;

        /// <summary>木元气浓�?/summary>
        public float Wood { get; set; } = 1f;

        /// <summary>水元气浓�?/summary>
        public float Water { get; set; } = 1f;

        /// <summary>火元气浓�?/summary>
        public float Fire { get; set; } = 1f;

        /// <summary>土元气浓�?/summary>
        public float Earth { get; set; } = 1f;

        /// <summary>总元气浓度（五行之和�?/summary>
        public float Total => Metal + Wood + Water + Fire + Earth;

        /// <summary>获取指定五行的浓�?/summary>
        public float Get(AuraElement element) => element switch
        {
            AuraElement.Metal => Metal,
            AuraElement.Wood  => Wood,
            AuraElement.Water => Water,
            AuraElement.Fire  => Fire,
            AuraElement.Earth => Earth,
            _ => 0f,
        };

        /// <summary>设置指定五行的浓�?/summary>
        public void Set(AuraElement element, float value)
        {
            switch (element)
            {
                case AuraElement.Metal: Metal = value; break;
                case AuraElement.Wood:  Wood  = value; break;
                case AuraElement.Water: Water = value; break;
                case AuraElement.Fire:  Fire  = value; break;
                case AuraElement.Earth: Earth = value; break;
            }
        }

        /// <summary>将另一个元气的值叠加到本元气（用于位面加成�?/summary>
        public void AddFrom(TileAura bonus)
        {
            Metal += bonus.Metal;
            Wood  += bonus.Wood;
            Water += bonus.Water;
            Fire  += bonus.Fire;
            Earth += bonus.Earth;
        }

        public override string ToString()
            => string.Format("Aura(金{0:F1} 木{1:F1} 水{2:F1} 火{3:F1} 土{4:F1})", Metal, Wood, Water, Fire, Earth);
    }

    /// <summary>五行元素枚举</summary>
    public enum AuraElement
    {
        Metal = 0,
        Wood  = 1,
        Water = 2,
        Fire  = 3,
        Earth = 4,
    }
}
