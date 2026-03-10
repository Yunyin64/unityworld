using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 游戏数据总管理器
    /// 负责在启动时统一初始化各数据子管理器，并作为全局访问入口
    ///
    /// 使用方式：
    ///   GameDataMgr.Instance.Traits.Get("brave")
    ///   GameDataMgr.Instance.Roles.Get("farmer")
    /// </summary>
    public class GameDataMgr
    {
        // ── 单例 ─────────────────────────────────────────────
        public static GameDataMgr Instance { get; private set; } = new();

        // ── 子管理器 ──────────────────────────────────────────
        private List<IDataMgrBase> _datamgrs  = new();

        // ── 初始化 ────────────────────────────────────────────

        public GameDataMgr (string? baseDir = null)
        {
            baseDir ??= AppContext.BaseDirectory;
            // 注册到列表，方便统一调用 Load
        _datamgrs.Add(new SocialRoleMgr(Path.Combine(baseDir, "SocialRoles.json")));
        _datamgrs.Add(new TraitDefineMgr(Path.Combine(baseDir, "Traits.json")));
        _datamgrs.Add(new NpcDefineMgr(Path.Combine(baseDir, "NpcDefines.json")));
        _datamgrs.Add(new PlaneDefineMgr(Path.Combine(baseDir, "PlaneDefines.json")));

            Instance = this;
        }

        /// <summary>
        /// 统一加载所有数据文件（应在游戏启动时调用一次）
        /// baseDir 默认为程序所在目录（与 Program.cs 同级）
        /// </summary>
        public static void Initialize(string? baseDir = null)
        {
            baseDir ??= AppContext.BaseDirectory;

            foreach (var mgr in Instance._datamgrs)
            {
                mgr.Load();
            }
            Console.WriteLine("[GameDataMgr] 所有数据加载完成");
        }
    }
}