using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    // ─────────────────────────────────────────────────────────────────────
    // 八大基础属性（修行者独有）
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 修行基础属性（八大属性）
    /// </summary>
    public struct BaseProperty
    {
        // ── 身体系 ──
        /// <summary>气血</summary>
        public int QiXue;

        /// <summary>体魄</summary>
        public int TiPo;

        // ── 灵感系 ──
        /// <summary>气感</summary>
        public int QiGan;

        /// <summary>灵机</summary>
        public int LingJi;

        // ── 精神系 ──
        /// <summary>神识</summary>
        public int ShenShi;

        /// <summary>悟性</summary>
        public int WuXing;

        // ── 命运系 ──
        /// <summary>机缘</summary>
        public int JiYuan;

        /// <summary>魅力</summary>
        public int MeiLi;

        /// <summary>创建全零的基础属性</summary>
        public static BaseProperty Zero => new BaseProperty
        {
            QiXue = 0, TiPo = 0,
            QiGan = 0, LingJi = 0,
            ShenShi = 0, WuXing = 0,
            JiYuan = 0, MeiLi = 0
        };

        /// <summary>凡人默认属性（全 10）</summary>
        public static BaseProperty Default => new BaseProperty
        {
            QiXue = 10, TiPo = 10,
            QiGan = 10, LingJi = 10,
            ShenShi = 10, WuXing = 10,
            JiYuan = 10, MeiLi = 10
        };
    }

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

        // ── 八大基础属性 ────────────────────────────────────

        /// <summary>修行基础属性</summary>
        public BaseProperty Properties { get; set; } = BaseProperty.Default;

        // ── 战斗三维 ────────────────────────────────────

        /// <summary>HP 上限</summary>
        public int HpMax { get; set; } = 10;

        /// <summary>MP 上限</summary>
        public int MpMax { get; set; } = 30;

        /// <summary>SP 上限（法力）</summary>
        public int SpMax { get; set; } = 10;

        // ── 战斗三维公式 ────────────────────────────────────

        /// <summary>
        /// 根据八大属性重算战斗三维
        /// HpMax = QiXue, MpMax = QiGan * 3, SpMax = ShenShi
        /// </summary>
        public void RecalcCombatStats()
        {
            HpMax = Properties.QiXue;
            MpMax = Properties.QiGan * 3;
            SpMax = Properties.ShenShi;
        }

        /// <summary>
        /// 根据 SoulData 认知功能映射五行亲和
        /// 水=FI+FE, 火=NI+NE, 金=TI+TE, 木=SI+SE, 土=MI+ME
        /// </summary>
        public void CalcAffinityFromSoul(SoulData soul)
        {
            Affinity = new ElementalAffinity
            {
                Shui = soul.FI + soul.FE,
                Huo = soul.NI + soul.NE,
                Jin = soul.TI + soul.TE,
                Mu = soul.SI + soul.SE,
                Tu = soul.MI + soul.ME
            };
        }

        // ── 五行亲和 ────────────────────────────────────

        /// <summary>五行亲和</summary>
        public ElementalAffinity Affinity { get; set; } = ElementalAffinity.Zero;

        public NpcGongFaData GongFaData { get; set; } = new();
        public NpcPraticeData PracticeData { get; set; } = new();
        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            LogMgr.Dbg("┌── CultivationData · 修行数据 ──────────────────────────");
            LogMgr.Dbg("│  道途:          {0}", Path.ToString());
            LogMgr.Dbg("│  寿元上限:      {0:F1} 年", LifespanMax);
            LogMgr.Dbg("│  八大属性:      气血={0} 体魄={1} 气感={2} 灵机={3} 神识={4} 悟性={5} 机缘={6} 魅力={7}",
                Properties.QiXue, Properties.TiPo, Properties.QiGan, Properties.LingJi,
                Properties.ShenShi, Properties.WuXing, Properties.JiYuan, Properties.MeiLi);
            LogMgr.Dbg("│  战斗三维:      HP={0} MP={1} SP={2}", HpMax, MpMax, SpMax);
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
        public int GetQixue()=>CultivationData.Properties.QiXue;
        public int GetTiPo()=>CultivationData.Properties.TiPo;
        public int GetQiGan()=>CultivationData.Properties.QiGan;
        public int GetLingJi()=>CultivationData.Properties.LingJi;
        public int GetShenShi()=>CultivationData.Properties.ShenShi;
        public int GetWuXing()=>CultivationData.Properties.WuXing;
        public int GetMeiLi()=>CultivationData.Properties.MeiLi;
        public int GetJiYuan()=>CultivationData.Properties.JiYuan;
        public int GetHpMax() => CultivationData.HpMax;
        public int GetMpMax() => CultivationData.MpMax;
        public int GetSpMax() => CultivationData.SpMax;
        public ElementalAffinity GetAffinity() => CultivationData.Affinity;
        public NpcGongFaData GongFaData =>CultivationData.GongFaData;
        public NpcPraticeData PracticeData =>CultivationData.PracticeData;
    }
}
