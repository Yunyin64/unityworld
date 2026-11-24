using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    // ─────────────────────────────────────────────────────────────────────
    // 八大基础属性（修行者独有）
    // ─────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    // 五行亲和
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 五行亲和（与五行元素的亲和度）
    /// </summary>
    public struct ElementalAffinity
    {
        /// <summary>金</summary>
        public int Jin;

        /// <summary>木</summary>
        public int Mu;

        /// <summary>水</summary>
        public int Shui;

        /// <summary>火</summary>
        public int Huo;

        /// <summary>土</summary>
        public int Tu;

        /// <summary>创建全零的五行亲和</summary>
        public static ElementalAffinity Zero => new ElementalAffinity
        {
            Jin = 0, Mu = 0, Shui = 0, Huo = 0, Tu = 0
        };

        /// <summary>把灵魂数据的十项数值两两相加，得到五行亲和</summary>
        public ElementalAffinity(SoulData soul)
        {
                Shui = soul.FI + soul.FE;
                Huo = soul.NI + soul.NE;
                Jin = soul.TI + soul.TE;
                Mu = soul.SI + soul.SE;
                Tu = soul.MI + soul.ME;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // NPC 修行数据
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NPC 修行运行时数据
    /// </summary>
    public class NpcCultivationData : IDomainDataBase
    {
        // ── 道途与境界 ────────────────────────────────────

        /// <summary>道途类型</summary>
        public PracticePath Path { get; set; } = PracticePath.None;
        // ── 寿元 ────────────────────────────────────

        /// <summary>寿元上限（修行延寿后的总值）</summary>
        public float LifespanMax { get; set; } = 80f;


        // ── 五行亲和 ────────────────────────────────────

        /// <summary>五行亲和</summary>
        public ElementalAffinity Affinity { get; set; } = ElementalAffinity.Zero;
        /// <summary>持有及激活的功法数据</summary>
        public NpcGongFaData GongFaData { get; set; } = new();
        /// <summary>当前修炼进度数据</summary>
        public NpcPracticeData PracticeData { get; set; } = new();

        /// <summary>复制修行数据和独立的功法、进度副本</summary>
        public NpcCultivationData Clone()
        {
            var copy = (NpcCultivationData)MemberwiseClone();
            copy.GongFaData = GongFaData.Clone();
            copy.PracticeData = PracticeData.Clone();
            return copy;
        }
        /// <summary>通过领域数据接口复制修行数据</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // ── 日志 ────────────────────────────────────

        /// <summary>记录修行道途、寿元和五行亲和等信息</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("┌── CultivationData · 修行数据 ──────────────────────────");
            LogMgr.Instance.Dbg("│  道途:          {0}", Path.ToString());
            LogMgr.Instance.Dbg("│  寿元上限:      {0:F1} 年", LifespanMax);
            LogMgr.Instance.Dbg("│  五行亲和:      金={0} 木={1} 水={2} 火={3} 土={4}",
                Affinity.Jin, Affinity.Mu, Affinity.Shui, Affinity.Huo, Affinity.Tu);
            LogMgr.Instance.Dbg("│  [=功法数据=]"); GongFaData.Log();
            LogMgr.Instance.Dbg("│  [=修炼数据=]"); PracticeData.Log();
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        /// <summary>读取角色当前修行道途</summary>
        public PracticePath GetPath() => CultivationData.Path;
        /// <summary>读取气血属性并转为整数</summary>
        public int GetQiXue()=> (int)Stats.Get("QiXue");
        /// <summary>读取体魄属性并转为整数</summary>
        public int GetTiPo()=> (int)Stats.Get("TiPo");
        /// <summary>读取气感属性并转为整数</summary>
        public int GetQiGan()=> (int)Stats.Get("QiGan");
        /// <summary>读取灵机属性并转为整数</summary>
        public int GetLingJi()=> (int)Stats.Get("LingJi");
        /// <summary>读取神识属性并转为整数</summary>
        public int GetShenShi()=> (int)Stats.Get("ShenShi");
        /// <summary>读取悟性属性并转为整数</summary>
        public int GetWuXing()=> (int)Stats.Get("WuXing");
        /// <summary>读取魅力属性并转为整数</summary>
        public int GetMeiLi()=> (int)Stats.Get("MeiLi");
        /// <summary>读取机缘属性并转为整数</summary>
        public int GetJiYuan()=> (int)Stats.Get("JiYuan");
        /// <summary>读取角色生命值上限</summary>
        public int GetHpMax() =>  (int)Stats.Get("HpMax");
        /// <summary>读取角色蓝条上限</summary>
        public int GetMpMax() =>  (int)Stats.Get("MpMax");
        /// <summary>读取角色可承载的体量上限</summary>
        public int GetSpMax() => (int)Stats.Get("SpMax");
        /// <summary>从属性表读取当前的五行亲和数值</summary>
        public ElementalAffinity GetAffinity() => new ElementalAffinity
        {
            Jin = (int)Stats.Get("AffinityJin"),
            Mu = (int)Stats.Get("AffinityMu"),
            Shui = (int)Stats.Get("AffinityShui"),
            Huo = (int)Stats.Get("AffinityHuo"),
            Tu = (int)Stats.Get("AffinityTu")
        };
        /// <summary>角色的功法数据</summary>
        public NpcGongFaData GongFa =>CultivationData.GongFaData;
        /// <summary>角色的修炼进度数据</summary>
        public NpcPracticeData PracticeData =>CultivationData.PracticeData;
    }
}
