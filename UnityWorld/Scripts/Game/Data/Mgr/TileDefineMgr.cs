namespace UnityWorld.Game.Data;

/// <summary>兼容既有定义查询；地图启动必须先由 MapCatalog 严格校验。</summary>
public sealed class TileDefineMgr : DefineMgrBase<TileDefine>
{
    public static TileDefineMgr Instance { get; private set; }
    public TileDefineMgr(string path) : base(path) => Instance = this;
}
