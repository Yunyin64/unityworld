using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 元素类型：包装基础元素枚举，支持拓展元素
    /// </summary>
    public struct ElementType
    {
        public BaseElementType Kind { get; private set; }

        public bool IsExtra => Kind == BaseElementType.Extra;

        public string ExtraTypeId { get; private set; }

        public static Dictionary<ElementType, int> ToDic(Dictionary<string, int> dic)
        {
            var ret = new Dictionary<ElementType, int>();
            foreach (var kv in dic)
            {
                ret[GetElementType(kv.Key)] = kv.Value;
            }
            return ret;
        }

        public  static ElementType None = new ElementType(BaseElementType.None,"None");
        public static ElementType Jin = new ElementType(BaseElementType.Jin,"Jin");
        public static ElementType Mu = new ElementType(BaseElementType.Mu,"Mu");
        public static ElementType Shui = new ElementType(BaseElementType.Shui,"Shui");
        public static ElementType Huo = new ElementType(BaseElementType.Huo,"Huo");
        public static ElementType Tu = new ElementType(BaseElementType.Tu,"Tu");
        public static ElementType Mix = new ElementType(BaseElementType.Extra,"Mix");

        
        public ElementType(BaseElementType kind, string id = "None")
        {
            Kind = kind;
            ExtraTypeId = id;
        }
         public override string ToString() => IsExtra ? ExtraTypeId : Kind.ToString();

        public static ElementType GetElementType(string id)
        {
            return id switch
            {
                "None" => ElementType.None,
                "Huo"  => ElementType.Huo,
                "Shui" => ElementType.Shui ,
                "Jin" => ElementType.Jin,
                "Mu"   => ElementType.Mu,
                "Tu"   => ElementType.Tu,
                _ => ExtraElementMgr.Instance?.Contains(id) == true
                        ? new ElementType(BaseElementType.Extra, id)
                        : new ElementType(BaseElementType.None),
            };
        }
    }
}