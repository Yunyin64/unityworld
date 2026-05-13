using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 装备定义数据管理器
    /// 负责加载 Data/Equip/ 文件夹下所有 JSON 文件并提供查询
    /// </summary>
    public class EquipDefineMgr : IDataMgrBase<EquipDefine>
    {
        public static EquipDefineMgr? Instance { get; private set; }

        private Dictionary<string, EquipDefine> _equips = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _folderPath;

        public EquipDefineMgr(string folderPath)
        {
            _folderPath = folderPath;
            Instance = this;
        }

        public void Load() => LoadFolder(_folderPath);

        public void Load(string filePath) => LoadFolder(filePath);

        /// <summary>
        /// 遍历指定文件夹下所有 *.json 文件，合并加载到 _equips
        /// </summary>
        private void LoadFolder(string folderPath)
        {
            _equips.Clear();

            if (!Directory.Exists(folderPath))
            {
                LogMgr.Warn("[EquipDefineMgr] 警告：找不到文件夹 {0}，装备定义库为空", folderPath);
                return;
            }

            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            if (jsonFiles.Length == 0)
            {
                LogMgr.Warn("[EquipDefineMgr] 警告：文件夹 {0} 下没有 JSON 文件", folderPath);
                return;
            }

            foreach (var file in jsonFiles)
            {
                var text = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(text))
                {
                    LogMgr.Dbg("[EquipDefineMgr] 跳过空文件：{0}", Path.GetFileName(file));
                    continue;
                }

                var list = JsonSerializer.Deserialize<List<EquipDefine>>(text, _jsonOpts);
                if (list == null || list.Count == 0)
                {
                    LogMgr.Dbg("[EquipDefineMgr] 文件无有效数据：{0}", Path.GetFileName(file));
                    continue;
                }

                foreach (var define in list)
                {
                    if (_equips.ContainsKey(define.ID))
                    {
                        LogMgr.Warn("[EquipDefineMgr] 重复 ID '{0}'（文件 {1}），已跳过",
                            define.ID, Path.GetFileName(file));
                        continue;
                    }
                    _equips[define.ID] = define;
                }

                LogMgr.Dbg("[EquipDefineMgr] 已加载 {0}：{1} 条", Path.GetFileName(file), list.Count);
            }

            LogMgr.Dbg("[EquipDefineMgr] 加载完成：共 {0} 个装备定义（来自 {1} 个文件）",
                _equips.Count, jsonFiles.Length);
        }

        public EquipDefine? Get(string id)
            => _equips.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<EquipDefine> GetAll() => _equips.Values;

        public bool Contains(string id) => _equips.ContainsKey(id);

        public IEnumerable<EquipDefine> Query(Func<EquipDefine, bool> predicate) => _equips.Values.Where(predicate);
    }
}
