using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 位面定义数据管理器
    /// </summary>
    public class PlaneDefineMgr : DefineMgrBase<PlaneDefine>
    {
        public static PlaneDefineMgr Instance { get; private set; }

        public PlaneDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        protected override JsonSerializerOptions CreateJsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
        };

        /// <summary>按位面类型筛选</summary>
        public IEnumerable<PlaneDefine> GetByKind(PlaneTypes.PlaneKind kind)
            => Query(d => d.Kind == kind);
    }
}
