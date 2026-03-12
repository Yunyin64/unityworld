using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 元素类型：包装基础元素枚举，支持拓展元素
    /// </summary>
    public class ElementType
    {
        public BaseElementType Kind { get; private set; }

        public bool IsExtra => Kind == BaseElementType.Extra;

        public string ExtraTypeId => IsExtra ? _extraId : "None";

        private string _extraId = "None";

        public static ElementType None = new ElementType(BaseElementType.None);

        public ElementType(BaseElementType kind, string id = "None")
        {
            Kind = kind;
            _extraId = id;
        }

        public static ElementType GetElementType(string id)
        {
            return id switch
            {
                "None" => new ElementType(BaseElementType.None),
                "Huo"  => new ElementType(BaseElementType.Huo),
                "Shui" => new ElementType(BaseElementType.Shui),
                "Jing" => new ElementType(BaseElementType.Jing),
                "Mu"   => new ElementType(BaseElementType.Mu),
                "Tu"   => new ElementType(BaseElementType.Tu),
                _ => ExtraElementMgr.Instance?.Contains(id) == true
                        ? new ElementType(BaseElementType.Extra, id)
                        : new ElementType(BaseElementType.None),
            };
        }
    }
}