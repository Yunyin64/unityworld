using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 元素类型：包装基础元素枚举，支持拓展元素
    /// </summary>
    public struct ElementType
    {
        /// <summary>元素所属的基础分类</summary>
        public BaseElementType Kind { get; private set; }

        /// <summary>是否为扩展元素</summary>
        public bool IsExtra => Kind == BaseElementType.Extra;

        /// <summary>扩展元素的编号</summary>
        public string ExtraTypeId { get; private set; }

        /// <summary>把以字符串为键的数量表转为元素类型键</summary>
        public static Dictionary<ElementType, int> ToDic(Dictionary<string, int> dic)
        {
            var ret = new Dictionary<ElementType, int>();
            foreach (var kv in dic)
            {
                ret[GetElementType(kv.Key)] = kv.Value;
            }
            return ret;
        }

        /// <summary>代表无元素</summary>
        public  static ElementType None = new ElementType(BaseElementType.None,"None");
        /// <summary>基础元素金</summary>
        public static ElementType Jin = new ElementType(BaseElementType.Jin,"Jin");
        /// <summary>基础元素木</summary>
        public static ElementType Mu = new ElementType(BaseElementType.Mu,"Mu");
        /// <summary>基础元素水</summary>
        public static ElementType Shui = new ElementType(BaseElementType.Shui,"Shui");
        /// <summary>基础元素火</summary>
        public static ElementType Huo = new ElementType(BaseElementType.Huo,"Huo");
        /// <summary>基础元素土</summary>
        public static ElementType Tu = new ElementType(BaseElementType.Tu,"Tu");
        /// <summary>扩展元素 Mix</summary>
        public static ElementType Mix = new ElementType(BaseElementType.Extra,"Mix");
        /// <summary>兼容旧配置的 Min 标识</summary>
        public static ElementType Min = new ElementType(BaseElementType.Extra,"Min");
        /// <summary>兼容旧配置的 Max 标识</summary>
        public static ElementType Max = new ElementType(BaseElementType.Extra,"Max");
        

        
        /// <summary>用基础分类和编号构造元素值</summary>
        public ElementType(BaseElementType kind, string id = "None")
        {
            Kind = kind;
            ExtraTypeId = id;
        }
         /// <summary>返回扩展编号或基础元素名</summary>
         public override string ToString() => IsExtra ? ExtraTypeId : Kind.ToString();

        /// <summary>按编号解析基础或扩展元素</summary>
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
        
        /// <summary>各基础元素的增益和减益编号</summary>
        public static Dictionary<ElementType,(string,string)> BaseElementBuff = new Dictionary<ElementType, (string, string)>
        {
                [ElementType.Jin] = ("Element_Buff_Jin","Element_Debuff_Jin"),
                [ElementType.Mu]   = ("Element_Buff_Mu","Element_Debuff_Mu"),
                [ElementType.Tu]   = ("Element_Buff_Tu","Element_Debuff_Tu"),
                [ElementType.Shui] = ("Element_Buff_Shui","Element_Debuff_Shui"),
                [ElementType.Huo]  = ("Element_Buff_Huo","Element_Debuff_Huo"),
        };

        /// <summary>随机返回一种基础五行增益或减益的编号</summary>
        public static string RandomBaseElementBuff(SoulData soul,bool isDebuff = false)
        {
            var kk = BaseElementBuff.Values.ToList()[soul.Random(BaseElementBuff.Count)];
            if(isDebuff) return kk.Item2;
            else return kk.Item1;
        }
    }
}