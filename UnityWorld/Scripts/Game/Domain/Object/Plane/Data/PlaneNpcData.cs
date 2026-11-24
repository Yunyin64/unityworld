namespace UnityWorld.Game.Domain;

/// <summary>位面内NPC归属数据，不在世界层再维护一份参与名单。</summary>
public sealed class PlaneNpcData
{
    /// <summary>当前位面成员的角色编号集合</summary>
    internal HashSet<int> Members { get; } = new();
    /// <summary>下次入位面的出生序号计数</summary>
    internal long NextBirthOrder { get; set; }
    /// <summary>位面成员编号的只读快照</summary>
    public IReadOnlyCollection<int> NpcIds => Array.AsReadOnly(Members.ToArray());
}
