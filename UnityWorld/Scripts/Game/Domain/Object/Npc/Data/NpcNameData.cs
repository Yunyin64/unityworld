using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 名字数据：存储 NPC 的姓、名、字、道号
    /// 
    /// 当前用途：作为 NpcBioData 的子字段（NpcBioData.NameData），
    /// 名字由 GlyphMgr 生成后存入此结构。
    /// 
    /// FullName 为只读计算属性，按"道号 + 姓 + 名"拼接。
    /// </summary>
    public class NpcNameData : IDomainDataBase
    {
        // ── 名字 ────────────────────────────────────

        /// <summary>姓</summary>
        public string Surname { get; set; } = "";

        /// <summary>名</summary>
        public string GivenName { get; set; } = "";

        /// <summary>字</summary>
        public string CourtesyName { get; set; } = "";

        /// <summary>道号（可随境界演变）</summary>
        public string DaoTitle { get; set; } = "";

        /// <summary>姓名（只读，拼接规则：姓 + 名）</summary>
        public string Name
        {
            get
            {
                return $"{Surname}{GivenName}";
            }
        }

        /// <summary>包含可选道号的完整显示名</summary>
        public string FullName
        {
            get
            {
                var dao = string.IsNullOrEmpty(DaoTitle) ? "" : DaoTitle + "·";
                return $"{dao}{Surname}{GivenName}";
            }
        }

        /// <summary>浅复制角色姓名数据</summary>
        public NpcNameData Clone()
        {
            var copy = (NpcNameData)MemberwiseClone();
            return copy;
        }
        /// <summary>通过领域数据接口复制姓名数据</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();


        // ── 日志 ────────────────────────────────────

        /// <summary>输出名字数据日志</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("│  [NameData] 姓: {0}  名: {1}  字: {2}  道号: {3}  全名: {4}",
                Surname, GivenName, CourtesyName, DaoTitle, FullName);
        }
    }
}