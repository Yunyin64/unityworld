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
        public NpcGongFaData GongFaData { get; set; } = new();
        public NpcPraticeData PracticeData { get; set; } = new();

        public NpcCultivationData Clone()
        {
            var copy = (NpcCultivationData)MemberwiseClone();
            copy.GongFaData = GongFaData.Clone();
            copy.PracticeData = PracticeData.Clone();
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            LogMgr.Dbg("┌── CultivationData · 修行数据 ──────────────────────────");
            LogMgr.Dbg("│  道途:          {0}", Path.ToString());
            LogMgr.Dbg("│  寿元上限:      {0:F1} 年", LifespanMax);
            LogMgr.Dbg("│  五行亲和:      金={0} 木={1} 水={2} 火={3} 土={4}",
                Affinity.Jin, Affinity.Mu, Affinity.Shui, Affinity.Huo, Affinity.Tu);
            LogMgr.Dbg("│  [=功法数据=]"); GongFaData.Log();
            LogMgr.Dbg("│  [=修炼数据=]"); PracticeData.Log();
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        public PracticePath GetPath() => CultivationData.Path;
        public int GetQixue()=> (int)Stats.Get("Qixue");
        public int GetTiPo()=> (int)Stats.Get("TiPo");
        public int GetQiGan()=> (int)Stats.Get("QiGan");
        public int GetLingJi()=> (int)Stats.Get("LingJi");
        public int GetShenShi()=> (int)Stats.Get("ShenShi");
        public int GetWuXing()=> (int)Stats.Get("WuXing");
        public int GetMeiLi()=> (int)Stats.Get("MeiLi");
        public int GetJiYuan()=> (int)Stats.Get("JiYuan");
        public int GetHpMax() =>  (int)Stats.Get("HpMax");
        public int GetMpMax() =>  (int)Stats.Get("MpMax");
        public int GetSpMax() => (int)Stats.Get("SpMax");
        public ElementalAffinity GetAffinity() => new ElementalAffinity
        {
            Jin = CultivationData.Affinity.Jin+(int)Stats.Get("AffinityJin"),
            Mu = CultivationData.Affinity.Mu+(int)Stats.Get("AffinityMu"),
            Shui = CultivationData.Affinity.Shui+(int)Stats.Get("AffinityShui"),
            Huo = CultivationData.Affinity.Huo+(int)Stats.Get("AffinityHuo"),
            Tu = CultivationData.Affinity.Tu+(int)Stats.Get("AffinityTu")
        };
        public NpcGongFaData GongFa =>CultivationData.GongFaData;
        public NpcPraticeData PracticeData =>CultivationData.PracticeData;
    }
}
