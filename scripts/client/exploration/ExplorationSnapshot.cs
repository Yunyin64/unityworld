using System.Collections.Generic;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace Xianxia.Client.Exploration;

/// <summary>Tile 来自核心提供的独立观察副本，名称来自同一世界的地形、地区和地标定义。</summary>
public sealed record ExplorationTile(Tile Tile, string Name, string Symbol, string Color,
    string RegionName, string LandmarkName, bool CanWalk)
{
    /// <summary>地标定义引用的 2D 图片逻辑地址。</summary>
    public string LandmarkImage { get; init; } = "";
    /// <summary>主地标在地图中的画法。</summary>
    public string LandmarkDrawType { get; init; } = "Icon";
    /// <summary>当前地形定义的图集或独立 PNG 引用。</summary>
    public string TerrainAssetRef { get; init; } = "";
    /// <summary>主地标以外的同格地标，按地标在模板中的顺序展示。</summary>
    public IReadOnlyList<string> AdditionalLandmarkNames { get; init; } = System.Array.Empty<string>();
    /// <summary>附加地标的 2D 图片地址，空字符串代表没有可用图。</summary>
    public IReadOnlyList<string> AdditionalLandmarkImages { get; init; } = System.Array.Empty<string>();
    /// <summary>附加地标在地图中的画法，与附加图片的顺序相同。</summary>
    public IReadOnlyList<string> AdditionalLandmarkDrawTypes { get; init; } = System.Array.Empty<string>();
}

/// <summary>一次读取的画面与命令依据。镜头和选中格由界面保存，可随时查看而不推进世界。</summary>
public sealed record ExplorationSnapshot(int Seed, int NpcId, string Name, string Identity,
    int PlaneId, TileId Position, WorldDate Date, NpcActionSnapshot Action, float Speed, double Radius,
    IReadOnlyDictionary<TileId, ExplorationTile> Tiles, IReadOnlySet<TileId> Candidates,
    bool CommandsEnabled, bool CanDrawActor, string Message)
{
    public bool CanContinueJourney => CommandsEnabled && CanDrawActor && Action.State == "move" && Action.Target.HasValue;
    public bool MadeTravelProgress { get; init; }
}
