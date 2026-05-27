using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// NPC 定义数据管理器
    /// </summary>
    public class NpcDefineMgr : DefineMgrBase<NpcDefine>
    {
        public static NpcDefineMgr Instance { get; private set; }

        public NpcDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        protected override JsonSerializerOptions CreateJsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        /// <summary>从已加载定义中随机获取一个</summary>
        public NpcDefine GetRandom(Random rng)
        {
            var values = GetAll().ToList();
            if (values.Count == 0) return null;
            return values[rng.Next(values.Count)];
        }
    }
}
