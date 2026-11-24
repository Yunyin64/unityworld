using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 凡间肉身数据（剥离修行、社会属性后的基础生物数据）
    /// </summary>
    public class NpcBioData:IDomainDataBase
    {
        // ── 身份核心（不变 / 创建时确定）─────────────────────────────

        /// <summary>名字数据（含完整显示名，未来扩展道号/绰号/称号）</summary>
        public NpcNameData NameData { get; set; } = new();
        // ── 外观 ────────────────────────────────────

        /// <summary>外观配置引用（ AppearanceData）</summary>
        public NpcAppearanceData AppearanceData { get; set; } = new();

        /// <summary>名字（只读代理，返回 NameData.Name 拼接结果）</summary>
        public string Name { get => NameData.Name; }
        /// <summary>含道号的角色全名</summary>
        public string FullName { get => NameData.FullName; }

        /// <summary>性别</summary>
        public NpcTypes.Gender Gender { get; set; }

        /// <summary>种族（人/妖/兽）</summary>
        public NpcTypes.NpcType NpcType { get; set; }

        // ── 生命周期 ────────────────────────────────────

        /// <summary>当前年龄积累（tick 推进，单位：年）</summary>
        public float AgeAccumulated { get; set; } = 0f;

        /// <summary>出生时的世界 Tick（可推算生辰）</summary>
        public int BirthTick { get; set; } = 0;

        // ── 体质 ────────────────────────────────────

        /// <summary>基础移动速度（凡人底值，TODO: 未来迁到 NpcSystemBehavior）</summary>
        public float BaseMoveSpeed { get; set; } = 3f;


        // ── 生死状态 ────────────────────────────────────
        // TODO: 未来迁到独立的生死 System

        /// <summary>是否存活</summary>
        public bool IsAlive { get; set; } = true;

        /// <summary>死亡时的 Tick（null = 活着）</summary>
        public int DeathTick { get; set; }

        /// <summary>复制生理数据及独立的姓名、外观副本</summary>
        public NpcBioData Clone()
        {
            var copy = (NpcBioData)MemberwiseClone();
            copy.NameData = NameData.Clone();
            copy.AppearanceData = AppearanceData.Clone();
            return copy;
        }
        /// <summary>通过领域数据接口复制生理数据</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        /// <summary>输出姓名、种族、年龄等角色生理信息</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("┌── Bio · 凡间肉身 ──────────────────────────");
            LogMgr.Instance.Dbg("│  名字:          {0}", Name);
            LogMgr.Instance.Dbg("│  性别:          {0}", Gender.ToString());
            LogMgr.Instance.Dbg("│  种族:          {0}", NpcType.ToString());
            LogMgr.Instance.Dbg("│  年龄(累计):    {0:F2} 年", AgeAccumulated);
            LogMgr.Instance.Dbg("│  出生Tick:      {0}", BirthTick);
            LogMgr.Instance.Dbg("│  基础移速:      {0:F2}", BaseMoveSpeed);
            LogMgr.Instance.Dbg("│  [=外观=]");            AppearanceData.Log();
            LogMgr.Instance.Dbg("│  存活:          {0}", IsAlive.ToString());
            LogMgr.Instance.Dbg("│  死亡Tick:      {0}", DeathTick.ToString());
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        /// <summary>读取角色的姓与名</summary>
        public string GetName() => BioData.Name;  
        /// <summary>读取角色性别</summary>
        public NpcTypes.Gender GetGender() => BioData.Gender;
        /// <summary>读取角色种族类型</summary>
        public NpcTypes.NpcType GetNpcType() => BioData.NpcType;
        /// <summary>角色的外观数据</summary>
        public NpcAppearanceData AppearanceData => BioData.AppearanceData;

    }
}