namespace UnityWorld.Game.Domain;

/// <summary>位面内NPC归属数据，不在世界层再维护一份参与名单。</summary>
public sealed class PlaneNpcData
{
    internal HashSet<int> Members { get; } = new();
    internal long NextBirthOrder { get; set; }
    public IReadOnlyCollection<int> NpcIds => Array.AsReadOnly(Members.ToArray());
}
