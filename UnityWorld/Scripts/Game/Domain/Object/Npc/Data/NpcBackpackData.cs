using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>一张已选入装配的卡及其公开坐标；未完成编辑的条目允许暂缺位置。</summary>
    public sealed class NpcBackpackEntry
    {
        /// <summary>持有卡实例 Id，不是卡定义 Id。</summary>
        public int CardId { get; set; }
        /// <summary>左上角锚点，从 (1,1) 起；候补不进入摆放条目。</summary>
        public BackpackPosition? Position { get; set; }
        /// <summary>复制条目，避免布局编辑修改原配置。</summary>
        public NpcBackpackEntry Clone() => new() { CardId = CardId, Position = Position };
    }

    /// <summary>NPC 的战前装配。只引用持有卡，不保存卡对象、灵元或扫描状态。</summary>
    public class NpcBackpackData : IDomainDataBase
    {
        /// <summary>由当前主功法配置确定的宽度。</summary>
        public int Width { get; set; }
        /// <summary>由当前主功法配置确定的高度。</summary>
        public int Height { get; set; }
        /// <summary>已经解析的有效空间，使用内部零起始坐标。</summary>
        public List<BackpackCell> ValidCells { get; set; } = [];
        /// <summary>主功法的固定占格。</summary>
        public List<BackpackCell> MainCardCells { get; set; } = [];
        /// <summary>卡实例和锚点的摆放条目；Field 等选择标记仍由 NpcCardData 维护。</summary>
        public List<NpcBackpackEntry> PlacedCards { get; set; } = [];
        /// <summary>查询装配中的锚点；不在场或尚未放置时返回 null。</summary>
        public BackpackPosition? GetPosition(int cardId) => PlacedCards.SingleOrDefault(entry => entry.CardId == cardId)?.Position;

        /// <summary>所有集合及条目深复制；战斗和编辑候选不共享世界配置。</summary>
        public NpcBackpackData Clone() => new()
        {
            Width = Width, Height = Height,
            ValidCells = [.. ValidCells], MainCardCells = [.. MainCardCells],
            PlacedCards = PlacedCards.Select(entry => entry.Clone()).ToList()
        };
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        public void Log()
        {
            
        }

    }
}
