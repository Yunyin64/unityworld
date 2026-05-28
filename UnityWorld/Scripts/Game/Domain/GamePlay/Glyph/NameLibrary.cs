using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 名字库数据模型（对应 NameLibrary.json）
    /// 提供姓氏、男名、女名、道号前缀、道号后缀等字符串池，
    /// 以及从 JSON 文件加载的静态方法。
    /// </summary>
    public class NameLibrary
    {
        /// <summary>姓氏库</summary>
        public string[] Surnames { get; set; } = [];

        /// <summary>男性名字库</summary>
        public string[] MaleFirstNames { get; set; } = [];

        /// <summary>女性名字库</summary>
        public string[] FemaleFirstNames { get; set; } = [];

        /// <summary>道号前缀库</summary>
        public string[] DaoTitlePrefixes { get; set; } = [];

        /// <summary>道号后缀库</summary>
        public string[] DaoTitleSuffixes { get; set; } = [];

        // ── 加载 ─────────────────────────────────────────────

        /// <summary>
        /// 从 JSON 文件加载名字库，加载失败时返回空库并输出警告日志
        /// </summary>
        /// <param name="path">JSON 文件路径</param>
        /// <returns>NameLibrary 实例（永不为 null）</returns>
        public static NameLibrary Load(string path)
        {
            if (!File.Exists(path))
            {
                LogMgr.Instance.Warn("[NameLibrary] 找不到名字库文件 {0}，将使用空库。", path);
                return new NameLibrary();
            }

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                return JsonSerializer.Deserialize<NameLibrary>(json, options)
                       ?? new NameLibrary();
            }
            catch (Exception ex)
            {
                LogMgr.Instance.Warn("[NameLibrary] 解析名字库失败：{0}", ex.Message);
                return new NameLibrary();
            }
        }
    }
}