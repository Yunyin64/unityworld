using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 物品运行时实例：ItemDefine 的实例化载体，承载可变状态
    /// </summary>
    public class Item : GameEntityBase, IFormDefine<ItemDefine>
    {
        /// <summary>实例 ID（= 所属 Card.Id）</summary>
        public int Id { get; set; }

        /// <summary>关联的 ItemDefine ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>显示名称</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// 从 ItemDefine 创建运行时实例
        /// </summary>
        public static Item FromDefine(int id, ItemDefine define)
        {
            return new Item
            {
                Id = id,
                DefineId = define.ID,
                DisplayName = define.DisplayName,
            };
        }

        public override void LogAllInfo()
        {
        }

        public override string ToString() => $"[Item:{DefineId}] {DisplayName}";
    }
}
