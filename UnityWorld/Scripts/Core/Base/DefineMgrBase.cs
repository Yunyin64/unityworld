using System.Text.Json;
using UnityWorld.Core;

/// <summary>
/// DefineMgr 泛型基类：统一 JSON 数据管理器的加载与查询逻辑。
/// 构造时传入路径（文件或文件夹均可），Load 时自动判断。
/// </summary>
public abstract class DefineMgrBase<TDefine> : IDataMgrBase<TDefine> where TDefine : DefineBase
{
    private Dictionary<string, TDefine> _dict = [];
    private readonly string _path;
    private JsonSerializerOptions _jsonOpts;

    /// <summary>数据目录路径（若构造时传入的是文件则取其所在目录）</summary>
    public string DataDir => Directory.Exists(_path) ? _path : Path.GetDirectoryName(_path);

    /// <summary>日志前缀，默认取子类类名</summary>
    protected virtual string MgrName => GetType().Name;

    /// <summary>
    /// 构造函数，接收数据路径（支持 .json 文件或文件夹）
    /// </summary>
    protected DefineMgrBase(string path)
    {
        _path = path;
        _jsonOpts = CreateJsonOptions();
    }

    /// <summary>
    /// 创建 JSON 反序列化选项。子类可 override 以添加 Converter 等。
    /// </summary>
    protected virtual JsonSerializerOptions CreateJsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>加载后钩子，子类可重写</summary>
    protected virtual void OnLoaded() { }

    // ── IDataMgrBase ─────────────────────────────────────────

    public void Load() => LoadPath(_path);

    /// <summary>从指定路径加载（支持 .json 文件或文件夹）</summary>
    public void Load(string path) => LoadPath(path);

    // ── 核心加载逻辑 ────────────────────────────────────────

    private void LoadPath(string path)
    {
        _dict.Clear();

        if (File.Exists(path))
        {
            LoadSingleFile(path);
        }
        else if (Directory.Exists(path))
        {
            LoadFolder(path);
        }
        else
        {
            LogMgr.Instance.Warn("[{0}] 警告：路径不存在 {1}，数据为空", MgrName, path);
            return;
        }

        OnLoaded();
        LogMgr.Instance.Dbg("[{0}] 加载完成：共 {1} 条定义", MgrName, _dict.Count);
    }

    private void LoadSingleFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(text))
        {
            LogMgr.Instance.Dbg("[{0}] 跳过空文件：{1}", MgrName, Path.GetFileName(filePath));
            return;
        }

        var list = JsonSerializer.Deserialize<List<TDefine>>(text, _jsonOpts);
        if (list == null || list.Count == 0) return;

        foreach (var define in list)
        {
            AddDefine(define, Path.GetFileName(filePath));
        }
    }

    private void LoadFolder(string folderPath)
    {
        var jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
        if (jsonFiles.Length == 0)
        {
            LogMgr.Instance.Warn("[{0}] 警告：文件夹 {1} 下没有 JSON 文件", MgrName, folderPath);
            return;
        }

        foreach (var file in jsonFiles)
        {
            LoadSingleFile(file);
        }
    }

    private void AddDefine(TDefine define, string sourceFile)
    {
        if (string.IsNullOrEmpty(define.ID)) return;

        if (_dict.ContainsKey(define.ID))
        {
            LogMgr.Instance.Warn("[{0}] 重复 ID '{1}'（文件 {2}），已跳过", MgrName, define.ID, sourceFile);
            return;
        }
        _dict[define.ID] = define;
    }

    // ── IDataMgrBase<TDefine> 查询接口 ──────────────────────

    public TDefine Get(string id)
        => _dict.TryGetValue(id, out var t) ? t : null;

    public IEnumerable<TDefine> GetAll() => _dict.Values;

    public bool Contains(string id) => _dict.ContainsKey(id);

    public IEnumerable<TDefine> Query(Func<TDefine, bool> predicate)
        => _dict.Values.Where(predicate);
}
