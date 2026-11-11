using System.Collections.Generic;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace Xianxia.Client.Exploration;

/// <summary>Tile 来自核心提供的独立观察副本，名称来自同一世界的地形、地区和地标定义。</summary>
public sealed record ExplorationTile(Tile Tile, string Name, string Symbol, string Color,
    string RegionName, string LandmarkName, bool CanWalk);

/// <summary>一次读取的画面与命令依据。镜头和选中格由界面保存，可随时查看而不推进世界。</summary>
public sealed record ExplorationSnapshot(int Seed, int NpcId, string Name, string Identity,
    int PlaneId, TileId Position, WorldDate Date, NpcActionSnapshot Action, float Speed, double Radius,
    IReadOnlyDictionary<TileId, ExplorationTile> Tiles, IReadOnlySet<TileId> Candidates,
    bool CommandsEnabled, bool CanDrawActor, string Message)
{
    public bool CanContinueJourney => CommandsEnabled && CanDrawActor && Action.State == "move" && Action.Target.HasValue;
    public bool MadeTravelProgress { get; init; }
}
