using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 天道铭刻管理器：管理天下所有实体的 Name 与外表（不存数据，只做创建）
    /// 
    /// 当前功能：
    ///   - 从 NameLibrary.json 加载名字库
    ///   - 随机生成姓名（道号+姓+名）
    ///   - 随机生成道号
    /// 
    /// 未来扩展：
    ///   - 称号/绰号/道号演变
    ///   - 外貌随机生成
    ///   - Tile/Region/Sect 等非 NPC 实体取名
    /// </summary>
    public class GlyphMgr : IGameplayMgrBase, ISoulBase
    {
        // ── 单例 ─────────────────────────────────────────────
        /// <summary>全局单例</summary>
        public static GlyphMgr Instance { get; private set; }

        // ── IGameplayMgrBase 属性 ───────────────────────────
        /// <summary>管理器名称</summary>
        public string Name => "GlyphMgr";

        /// <summary>管理器描述</summary>
        public string Desc => "天道铭刻管理器，管理天下所有实体的Name与外表生成";

        // ── ISoulBase ────────────────────────────────────────
        /// <summary>灵魂数据（含 Rng）</summary>
        public SoulData Soul { get; }

        // ── 内部数据 ─────────────────────────────────────────
        private NameLibrary _nameLibrary = new();
        private Rng _rng;

        // ── 构造函数 ─────────────────────────────────────────

        /// <summary>
        /// 构造 GlyphMgr，初始化 SoulData 和 Rng
        /// </summary>
        /// <param name="seed">随机种子</param>
        public GlyphMgr(int seed)
        {
            Soul = new SoulData(seed);
            _rng = new Rng(seed);
            Instance = this;

            // 构造时即加载名字库（_gameplays 目前未统一调 Init）
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "NameLibrary.json");
            _nameLibrary = NameLibrary.Load(path);
        }

        // ── 名字生成（纯工厂，不存数据）─────────────────────

        /// <summary>
        /// 随机生成完整姓名（道号+姓+名）
        /// </summary>
        /// <param name="gender">性别（决定名字池选择）</param>
        /// <returns>格式："道号前缀道号后缀·姓名"</returns>
        public string RandomSurname()
        {
            string surname = RandomFrom(_nameLibrary.Surnames, "佚");
            return surname;
        }

        public string RandomGivenname(NpcTypes.Gender gender)
        {
            string[] firstPool = gender == NpcTypes.Gender.Female
                ? _nameLibrary.FemaleFirstNames
                : _nameLibrary.MaleFirstNames;
            string firstName = RandomFrom(firstPool, "名");
            return firstName;
        }


        /// <summary>
        /// 随机生成道号（前缀+后缀，如"青玄子"）
        /// </summary>
        /// <returns>道号字符串</returns>
        public string RandomDaoTitle()
        {
            string prefix = RandomFrom(_nameLibrary.DaoTitlePrefixes, "无极");
            string suffix = RandomFrom(_nameLibrary.DaoTitleSuffixes, "子");
            return prefix + suffix;
        }

        // ── 私有工具 ─────────────────────────────────────────

        /// <summary>从字符串池中随机选取一个，池为空时返回 fallback</summary>
        private string RandomFrom(string[] pool, string fallback)
        {
            if (pool == null || pool.Length == 0) return fallback;
            return pool[_rng.Range(0, pool.Length)];
        }

        // ── 生命周期方法 ─────────────────────────────────────

        /// <summary>无耦合初始化</summary>
        public void Init()
        {
            LogMgr.Instance.Dbg("[GlyphMgr] 初始化完成，名字库已加载");
        }

        /// <summary>有耦合初始化</summary>
        public void Begin() { }

        /// <summary>帧更新（GlyphMgr 不存数据，无需 Tick）</summary>
        public void Tick(float deltaTime) { }

        /// <summary>帧更新</summary>
        public void Update() { }

        /// <summary>渲染更新</summary>
        public void Render(float dt) { }

        /// <summary>结束/销毁</summary>
        public void End()
        {
            Instance = null;
        }

        public BirthContext GeneratorNpc(BirthContext ctx)
        {
            var npc = ctx.MainNpc;
            var gender = (NpcTypes.Gender)npc.Soul.Random(0, 2);
            ctx.Set("Gender", gender);
            ctx.Set("Surname", RandomSurname());
            ctx.Set("GivenName", RandomGivenname(gender));
            ctx.Set("DaoTitle", RandomDaoTitle());
            


            return ctx;
        }

        /// <summary>日志输出：名字库统计信息</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[GlyphMgr] 名字库统计：");
            LogMgr.Instance.Dbg("  姓氏: {0} 条", _nameLibrary.Surnames.Length);
            LogMgr.Instance.Dbg("  男名: {0} 条", _nameLibrary.MaleFirstNames.Length);
            LogMgr.Instance.Dbg("  女名: {0} 条", _nameLibrary.FemaleFirstNames.Length);
            LogMgr.Instance.Dbg("  道号前缀: {0} 条", _nameLibrary.DaoTitlePrefixes.Length);
            LogMgr.Instance.Dbg("  道号后缀: {0} 条", _nameLibrary.DaoTitleSuffixes.Length);
        }
    }
}