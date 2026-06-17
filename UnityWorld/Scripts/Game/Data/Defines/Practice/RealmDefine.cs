namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 境界定于：每个道途拥有独立的境界序列
    /// 如灵修：练气→筑基→金丹→...；武修：锻体→铜皮→铁骨→...
    /// </summary>
    public class RealmDefine : DefineBase
    {
        /// <summary>所属道途类型</summary>
        public PracticePath Type { get; set; } = PracticePath.None;

        /// <summary>境界等级（同一道途内的顺序，1 为最低）</summary>
        public int Level { get; set; } = 1;

        /// <summary>突破所需进度值</summary>
        public int ProgressRequired { get; set; } = 100;

        /// <summary>突破后寿命加成（百分比）</summary>
        public float LifespanBonus { get; set; } = 0f;
    }
}
