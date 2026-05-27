using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 功法定义数据管理器
    /// </summary>
    public class CultivationDefineMgr : DefineMgrBase<CultivationDefine>
    {
        public static CultivationDefineMgr Instance { get; private set; }

        public CultivationDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        protected override JsonSerializerOptions CreateJsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
        };

        /// <summary>获取指定道途的所有功法</summary>
        public IEnumerable<CultivationDefine> GetByPath(PracticePath path)
            => Query(d => d.PathType == path);

        /// <summary>获取指定道途和境界的功法</summary>
        public IEnumerable<CultivationDefine> GetByPathAndRealm(PracticePath path, int realmLevel)
            => Query(d => d.PathType == path && (d.RealmLevel == 0 || d.RealmLevel == realmLevel));
    }
}
