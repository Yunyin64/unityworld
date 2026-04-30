using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Card 模板定义数据管理器
    /// 负责加载 Data/Card/ 文件夹下所有 JSON 文件并提供查询
    /// </summary>
    public class CardDefineMgr : IDataMgrBase<CardDefine>
    {
        public static CardDefineMgr? Instance { get; private set; }

        private Dictionary<string, CardDefine> _cards = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _folderPath;

        public CardDefineMgr(string folderPath)
        {
            _folderPath = folderPath;
            Instance = this;
        }

        public void Load() => LoadFolder(_folderPath);

        public void Load(string filePath) => LoadFolder(filePath);

        /// <summary>
        /// 遍历指定文件夹下所有 *.json 文件，合并加载到 _cards
        /// </summary>
        private void LoadFolder(string folderPath)
        {
            _cards.Clear();

            if (!Directory.Exists(folderPath))
            {
                LogMgr.Warn("[CardDefineMgr] 警告：找不到文件夹 {0}，Card模板库为空", folderPath);
                return;
            }

            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            if (jsonFiles.Length == 0)
            {
                LogMgr.Warn("[CardDefineMgr] 警告：文件夹 {0} 下没有 JSON 文件", folderPath);
                return;
            }

            foreach (var file in jsonFiles)
            {
                var text = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(text))
                {
                    LogMgr.Dbg("[CardDefineMgr] 跳过空文件：{0}", Path.GetFileName(file));
                    continue;
                }

                var list = JsonSerializer.Deserialize<List<CardDefine>>(text, _jsonOpts);
                if (list == null || list.Count == 0)
                {
                    LogMgr.Dbg("[CardDefineMgr] 文件无有效数据：{0}", Path.GetFileName(file));
                    continue;
                }

                foreach (var define in list)
                {
                    if (_cards.ContainsKey(define.ID))
                    {
                        LogMgr.Warn("[CardDefineMgr] 重复 ID '{0}'（文件 {1}），已跳过",
                            define.ID, Path.GetFileName(file));
                        continue;
                    }
                    _cards[define.ID] = define;
                }

                LogMgr.Dbg("[CardDefineMgr] 已加载 {0}：{1} 条", Path.GetFileName(file), list.Count);
            }

            LogMgr.Dbg("[CardDefineMgr] 加载完成：共 {0} 个Card模板定义（来自 {1} 个文件）",
                _cards.Count, jsonFiles.Length);
        }

        public CardDefine? Get(string id)
            => _cards.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<CardDefine> GetAll() => _cards.Values;



        public bool Contains(string id) => _cards.ContainsKey(id);

        public IEnumerable<CardDefine> Query(Func<CardDefine, bool> predicate) => _cards.Values.Where(predicate);
    }
}
