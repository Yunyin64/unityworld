using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 境界定义数据管理器
    /// </summary>
    public class RealmDefineMgr : DefineMgrBase<RealmDefine>
    {
        public static RealmDefineMgr Instance { get; private set; }

        public RealmDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        protected override JsonSerializerOptions CreateJsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        /// <summary>获取指定道途的所有境界（按 Level 排序）</summary>
        public IEnumerable<RealmDefine> GetByPath(PracticePath path)
            => Query(d => d.Type == path).OrderBy(d => d.Level);

        /// <summary>获取指定道途和等级的境界</summary>
        public RealmDefine GetByPathAndLevel(PracticePath path, int level)
            => Query(d => d.Type == path && d.Level == level).FirstOrDefault();
    }
}
