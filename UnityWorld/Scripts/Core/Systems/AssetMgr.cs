using System.Collections;

namespace UnityWorld.Core
{
    /// <summary>按内容包挂载顺序预载和解析资源，由宿主解释具体类型。</summary>
    public class AssetMgr : IDomainMgrBase
    {
        /// <summary>宿主提供的资源包枚举和读取入口。</summary>
        public sealed record Source(string Id, Func<IEnumerable<string>> List, Func<string, byte[]> Read);

        /// <summary>预载后的资源内容及其来源。</summary>
        public sealed record Asset(string SourceId, string Path, ReadOnlyMemory<byte> Bytes);

        private Dictionary<string, IReadOnlyList<Asset>> contents = new(StringComparer.Ordinal);

        /// <summary>当前世界的资源目录。</summary>
        public static AssetMgr Instance { get; private set; }

        /// <summary>每个逻辑地址优先级最高的已读资源。</summary>
        public IEnumerable<Asset> Assets => contents.Values.Select(assets => assets[0]);

        /// <summary>预载中的错误与覆盖记录。</summary>
        public IReadOnlyList<string> Diagnostics { get; private set; } = Array.Empty<string>();

        /// <summary>领域管理器名称。</summary>
        public string Name => "AssetMgr";

        /// <summary>领域管理器描述。</summary>
        public string Desc => "";

        /// <summary>新建独立资源目录。</summary>
        public AssetMgr() { }

        /// <summary>共享已预载快照，各目录独立释放引用。</summary>
        public AssetMgr(AssetMgr prepared)
        {
            contents = prepared.contents;
            Diagnostics = prepared.Diagnostics;
        }

        /// <summary>注册当前世界的资源目录。</summary>
        public void Init() => Instance = this;

        /// <summary>释放本目录并注销当前世界的引用。</summary>
        public void End()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
            contents = new(StringComparer.Ordinal);
            Diagnostics = Array.Empty<string>();
        }

        /// <summary>将可选的文件系统 src 目录挂载为一个资源包。</summary>
        public static Source FromDirectory(string id, string root)
        {
            root = Path.GetFullPath(root);
            return new(id,
                () => !Directory.Exists(root) ? Array.Empty<string>() :
                    Directory.EnumerateFiles(root, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        AttributesToSkip = FileAttributes.ReparsePoint
                    }).Select(file => Path.GetRelativePath(root, file)),
                path => File.ReadAllBytes(Path.Combine(root, path)));
        }

        /// <summary>规范化 src 相对地址，非法地址返回空值。</summary>
        public static string NormalizeAddress(string path)
        {
            string address = path?.Trim().Replace('\\', '/') ?? "";
            if (address.StartsWith("src/", StringComparison.OrdinalIgnoreCase)) address = address[4..];
            if (address.Contains(':') || address.Any(char.IsControl) ||
                address.Split('/').Any(part => part is "" or "." or "..")) return null;
            return address;
        }

        /// <summary>重新加载挂载目录，跳过失败的资源并保留诊断。</summary>
        public void Preload(IEnumerable<Source> sources, IEnumerable<string> extensions)
        {
            // 先构造下一份目录，成功读到的后挂载资源排在前面，再整体替换当前快照。
            var supported = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            var next = new Dictionary<string, List<Asset>>(StringComparer.Ordinal);
            var diagnostics = new List<string>();
            var mounted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                if (string.IsNullOrWhiteSpace(source.Id) || !mounted.Add(source.Id))
                {
                    diagnostics.Add($"无效或重复的资源包 ID：{source.Id}");
                    continue;
                }

                try
                {
                    var files = source.List().OrderBy(path => path, StringComparer.Ordinal).ToArray();
                    var addresses = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string file in files)
                    {
                        string address = NormalizeAddress(file);
                        if (address == null || !addresses.Add(address))
                        {
                            diagnostics.Add($"[{source.Id}] 无效或重复的资源地址：{file}");
                            continue;
                        }
                        if (!supported.Contains(Path.GetExtension(address))) continue;
                        try
                        {
                            byte[] bytes = source.Read(file);
                            if (bytes is not { Length: > 0 }) throw new InvalidDataException("资源为空。");
                            if (!next.TryGetValue(address, out var candidates))
                                next.Add(address, candidates = new());
                            else
                                diagnostics.Add($"[{source.Id}] {address} 覆盖 {candidates[0].SourceId}");
                            candidates.Insert(0, new Asset(source.Id, address, bytes));
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
                        {
                            diagnostics.Add($"[{source.Id}] {address} 读取失败：{ex.Message}");
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    diagnostics.Add($"[{source.Id}] 枚举资源失败：{ex.Message}");
                }
            }
            contents = next.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Asset>)pair.Value.AsReadOnly(), StringComparer.Ordinal);
            Diagnostics = diagnostics.AsReadOnly();
        }

        /// <summary>从优先级最高的来源起返回候选，供宿主在解码失败时回退。</summary>
        public IReadOnlyList<Asset> Find(string path)
        {
            string address = NormalizeAddress(path);
            return address != null && contents.TryGetValue(address, out var candidates) ? candidates : Array.Empty<Asset>();
        }

        /// <summary>清理旧式路径，兼容分号列表的原调用。</summary>
        public static string GetFixedPath(string path, int index = -1)
        {
            if (string.IsNullOrEmpty(path)) return "";
            path = path.Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace('\\', '/');
            return (index == -1 ? path : path.Split(';')[index]).Trim();
        }

        /// <summary>领域启动回调。</summary>
        public void Begin() { }

        /// <summary>领域时步回调。</summary>
        public void Tick(float deltaTime) { }

        /// <summary>领域更新回调。</summary>
        public void Update() { }

        /// <summary>领域渲染回调。</summary>
        public void Render(float dt) { }

        /// <summary>内容包资源无需写入存档。</summary>
        public IEnumerator Save() { yield break; }

        /// <summary>资源由启动预载填充。</summary>
        public IEnumerator Load() { yield break; }

        /*
public Sprite GetSprite(string path,int layout)
{
    path = GetFixedPath(path, -1);
    try
    {
        if (this.m_mapSprites.ContainsKey(path))
        {
            return this.m_mapSprites[path];
        }
        if (path.IndexOf('.') <= 0)
        {
            int num = path.LastIndexOf("/");
            string text = path;
            if (num >= 0)
            {
                text = path.Substring(0, num);
            }
            if (!this.m_lisLoadedPath.Contains(text))
            {
                Sprite[] array = Resources.LoadAll<Sprite>(text);
                for (int i = 0; i < array.Length; i++)
                {
                    this.m_mapSprites[text + "/" + array[i].name] = array[i];
                }
                this.m_lisLoadedPath.Add(text);
                if (this.m_mapSprites.ContainsKey(path))
                {
                    return this.m_mapSprites[path];
                }
            }
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                this.m_mapSprites[path] = sprite;
                return sprite;
            }
        }
    }
    catch (Exception ex)
    {
        LogMgr.Instance.Err(string.Concat(new object[]
        {
        "Sprite Error:",
        path,
        "\n",
        ex.Message,
        "\n",
        ex.InnerException
        }), new object[0]);
        path = null;
    }
    Texture2D texture2D = this.GetTexture2D(path);
    if (texture2D != null)
    {
        Sprite sprite2 = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0U, SpriteMeshType.Tight);
        this.m_mapSprites[path] = sprite2;
        return sprite2;
    }
    return null;
}
public SpriteRenderer GetIconSprFromPool(string name)
{
    List<SpriteRenderer> list = null;
    if (this.m_IconPool.TryGetValue(name, out list) && list.Count > 0)
    {
        SpriteRenderer result = list[0];
        list.RemoveAt(0);
        return result;
    }
    GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("CMDIcon")) as GameObject;
    gameObject.name = name;
    SpriteRenderer componentInChildren = gameObject.GetComponentInChildren<SpriteRenderer>();
    componentInChildren.sprite = this.GetSprite(name,0);
    return componentInChildren;
}
public Texture2D GetTexture2D(string path)
{
    if (string.IsNullOrEmpty(path))
    {
        return null;
    }
    Texture2D result;
    if (path.IndexOf('.') > 0)
    {
        result = ModsMgr.Instance.LoadTexture2D("Resources/" + path);
    }
    else
    {
        result = Resources.Load<Texture2D>(path);
    }
    return result;
}

private Dictionary<string, List<SpriteRenderer>> m_IconPool = new Dictionary<string, List<SpriteRenderer>>();

private Dictionary<string, Sprite> m_mapSprites = new Dictionary<string, Sprite>();

*/
        private List<string> m_lisLoadedPath = new List<string>();
	}
}
