using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 性格运行时数据（用于驱动决策权重的八维人格倾向）
    /// </summary>
    public class NpcPersonalityData : IDomainDataBase
    {
        // ── 性格八维 ────────────────────────────────────

        /// <summary>冒险倾向</summary>
        public float Boldness { get; set; } = 0f;

        /// <summary>同情心</summary>
        public float Compassion { get; set; } = 0f;

        /// <summary>贪婪</summary>
        public float Greed { get; set; } = 0f;

        /// <summary>荣誉感</summary>
        public float Honor { get; set; } = 0f;

        /// <summary>理性</summary>
        public float Rationality { get; set; } = 0f;

        /// <summary>社交性</summary>
        public float Sociability { get; set; } = 0f;

        /// <summary>复仇心</summary>
        public float Vengefulness { get; set; } = 0f;

        /// <summary>狂热</summary>
        public float Zeal { get; set; } = 0f;

        public NpcPersonalityData Clone()
        {
            var copy = (NpcPersonalityData)MemberwiseClone();
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // ── 日志 ────────────────────────────────────

        /// <summary>
        /// 日志输出
        /// </summary>
        public void Log()
        {
            LogMgr.Dbg("┌── PersonalityData ──────────────────────────");
            LogMgr.Dbg("│  冒险倾向:      {0:F2}", Boldness);
            LogMgr.Dbg("│  同情心:        {0:F2}  贪婪:        {1:F2}", Compassion, Greed);
            LogMgr.Dbg("│  荣誉感:        {0:F2}  理性:        {1:F2}", Honor, Rationality);
            LogMgr.Dbg("│  社交性:        {0:F2}  复仇心:      {1:F2}", Sociability, Vengefulness);
            LogMgr.Dbg("│  狂热:          {0:F2}", Zeal);
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        /// <summary>获取冒险倾向</summary>
        public float GetBoldness() => PersonalityData.Boldness;

        /// <summary>获取同情心</summary>
        public float GetCompassion() => PersonalityData.Compassion;

        /// <summary>获取贪婪</summary>
        public float GetGreed() => PersonalityData.Greed;

        /// <summary>获取荣誉感</summary>
        public float GetHonor() => PersonalityData.Honor;

        /// <summary>获取理性</summary>
        public float GetRationality() => PersonalityData.Rationality;

        /// <summary>获取社交性</summary>
        public float GetSociability() => PersonalityData.Sociability;

        /// <summary>获取复仇心</summary>
        public float GetVengefulness() => PersonalityData.Vengefulness;

        /// <summary>获取狂热</summary>
        public float GetZeal() => PersonalityData.Zeal;
    }
}