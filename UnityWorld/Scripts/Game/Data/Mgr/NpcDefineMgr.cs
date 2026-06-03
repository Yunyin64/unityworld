using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Game.Domain;

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

        /// <summary>
        /// 将 NpcDefine 转换为 BirthContext，按已有字段填充。
        /// </summary>
        public static BirthContext ToBirthContext(NpcDefine define)
        {
            var ctx = new BirthContext();
            ctx.Set("NpcType", define.NpcType);
            ctx.Set("Gender", define.Gender);
            ctx.Set("Surname", define.Surname.Length > 0 ? define.Surname : define.DisplayName);
            ctx.Set("GivenName", define.GivenName);
            ctx.Set("DaoTitle", define.DaoTitle);
            ctx.Set("InitAge", define.InitAge);
            ctx.Set("InitStat", define.InitStat);
            ctx.Set("InitCardDeck", define.InitCardDeck);
            ctx.Set("InitTraits", define.InitialTraits);
            return ctx;
        }
    }
}
