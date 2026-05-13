using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 装备运行时实例：EquipDefine 的实例化载体。
    /// Define 只是模板（Base 值），Equip 是最终生效的运行时对象。
    /// 字段去掉 Base 后缀，表示可被 Modifier 等机制修改的最终值。
    /// </summary>
    public class Equip : IFormDefine<EquipDefine>
    {
        /// <summary>关联的 EquipDefine ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>显示名称</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>作用于哪个 Size 的卡牌</summary>
        public int Size { get; set; } = 1;

        /// <summary>攻击值（最终生效值，可被修改器叠加）</summary>
        public int Attack { get; set; } = 0;

        /// <summary>防御值（最终生效值）</summary>
        public int Defend { get; set; } = 0;

        /// <summary>速度（最终生效值）</summary>
        public float Speed { get; set; } = 0;

        /// <summary>数量/耐久（最终生效值）</summary>
        public int Amount { get; set; } = 1;

        /// <summary>招式卡列表（最终生效值，引用 CardDefine.ID）</summary>
        public List<string> FormList { get; set; } = [];

        /// <summary>
        /// 从 EquipDefine 创建运行时实例，将 Base 值复制为初始最终值
        /// </summary>
        public static Equip FromDefine(EquipDefine define)
        {
            return new Equip
            {
                DefineId = define.ID,
                DisplayName = define.DisplayName,
                Size = define.Size,
                Attack = define.AttackBase,
                Defend = define.DefendBase,
                Speed = define.SpeedBase,
                Amount = define.AmountBase,
                FormList = new List<string>(define.FormListBase),
            };
        }

        public override string ToString() => $"[Equip:{DefineId}] {DisplayName} Atk={Attack} Def={Defend} Spd={Speed}";
    }
}
