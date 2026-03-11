using System.Text.Json;
using UnityWorld.Game.Common.Math;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 名字库数据模型（对应 NameLibrary.json�?
    /// </summary>
    public class NameLibraryData
    {
        public string[] Surnames { get; set; } = [];
        public string[] MaleFirstNames { get; set; } = [];
        public string[] FemaleFirstNames { get; set; } = [];
        public string[] DaoTitlePrefixes { get; set; } = [];
        public string[] DaoTitleSuffixes { get; set; } = [];
    }

    /// <summary>
    /// 名字系统：从外部 NameLibrary.json 加载姓名库与道号库，
    /// 提供随机生成姓名、道号的功能�?
    ///
    /// 命名规则�?
    ///   凡人：姓 + 名（如：李大柱）
    ///   修士：道号前缀 + 道号后缀·�?+ 名（如：青玄子·李大柱�?
    /// </summary>
    public class NpcSystemName : NpcSystemBase
    {
        private readonly NameLibraryData _library;
        private readonly Rng _rng;

        // 名字存储�?
        private readonly Dictionary<NpcId, string> _nameTable = new();

        /// <summary>
        /// 构造函数，加载 NameLibrary.json
        /// </summary>
        /// <param name="rng">随机数生成器（复用NpcMgr的Rng�?/param>
        /// <param name="jsonPath">json文件路径，默认为 NameLibrary.json（与exe同级�?/param>
        public NpcSystemName(Rng rng, string? jsonPath = null)
        {
            _rng = rng;
            jsonPath ??= Path.Combine(AppContext.BaseDirectory, "Data", "NameLibrary.json");
            _library = LoadLibrary(jsonPath);
        }

        // ── 注册与查�?───────────────────────────────────────────

        /// <summary>注册NPC姓名（创建时调用�?/summary>
        public void Register(Npc npc, string fullName)
        {
            _nameTable[npc.Id] = fullName;
        }

        /// <summary>获取NPC的完整显示名（含道号�?/summary>
        public string? GetName(NpcId id)
        {
            return _nameTable.TryGetValue(id, out var name) ? name : null;
        }

        // ── 随机生成 ─────────────────────────────────────────────

        /// <summary>
        /// 随机生成凡人姓名（姓 + 名）
        /// </summary>
        public string RandomName(NpcTypes.Gender gender)
        {
            string surname = RandomFrom(_library.Surnames, "佚");
            string[] firstPool = gender == NpcTypes.Gender.Female
                ? _library.FemaleFirstNames
                : _library.MaleFirstNames;
            string firstName = RandomFrom(firstPool, "名");
            return surname + firstName;
        }

        /// <summary>
        /// 随机生成道号（前缀 + 后缀，如"青玄�?�?
        /// </summary>
        public string RandomDaoTitle()
        {
            string prefix = RandomFrom(_library.DaoTitlePrefixes, "无极");
            string suffix = RandomFrom(_library.DaoTitleSuffixes, "子");
            return prefix + suffix;
        }

        /// <summary>
        /// 随机生成修士完整称号（道号·姓名，�?青玄子·李大柱"�?
        /// </summary>
        public string RandomCultivatorFullName(NpcTypes.Gender gender)
        {
            string daoTitle = RandomDaoTitle();
            string name = RandomName(gender);
            return $"{daoTitle}·{name}";
        }

        // ── Tick（名字系统不需要Tick逻辑，保留接口） ────────────
        public override void OnTick(Npc npc, float deltaTime) { }

        // ── 私有工具 ─────────────────────────────────────────────

        private string RandomFrom(string[] pool, string fallback)
        {
            if (pool == null || pool.Length == 0) return fallback;
            return pool[_rng.Range(0, pool.Length)];
        }

        private static NameLibraryData LoadLibrary(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[NameSystem] 警告：找不到名字库文件 {path}，将使用空库。");
                return new NameLibraryData();
            }

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                return JsonSerializer.Deserialize<NameLibraryData>(json, options)
                       ?? new NameLibraryData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NameSystem] 解析名字库失败：{ex.Message}");
                return new NameLibraryData();
            }
        }
    }
}
