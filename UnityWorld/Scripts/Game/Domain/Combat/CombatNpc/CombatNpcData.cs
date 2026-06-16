using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        // ── 战斗 ──────────────────────────────────
        /// <summary>
        /// 战斗内当前数值（与大世界 StatBlock 独立）
        /// </summary>
        /// 
        public  int ShieldValue { get; private  set; }
        private int Hp { get; set; }
        private int Mp { get; set; }
        private int Sp { get ; set; }

        public int GetHp(){return (int)Hp;}
        public int GetCombatHpMax(){return GetHpMax();}
        public int GetMp(){return (int)Mp;}
        public int GetCombatMpMax(){return GetMpMax();}
        public int GetSp(){
            int sp = 0;
            foreach (var kv in Field) sp += kv.GetSize();
            return  sp;}
        public int GetCombatSpMax(){return GetSpMax();}

        public void ChangeShield(int val)
        {
            //可能要加入护盾上限值，待定
            ShieldValue += val;
            ShieldValue = Math.Clamp(ShieldValue, 0, int.MaxValue);
            Log($"护盾值变更：{val:+0.##;-0.##;0}，当前护盾={ShieldValue:0.##}");
        }
        public void InitData()
        {
            // 从大世界 Npc 读取 HP/SP/MP
            Hp = GetCombatHpMax();
            Mp = GetCombatMpMax();
            
            Log(string.Format("NPC {0}, HP={1}/{2}, MP={3}/{4}, SP={5}/{6}",GetName(),
            Hp,GetCombatHpMax(),Mp,GetCombatMpMax(),Sp,GetCombatSpMax()));
        }

        // ── 招式轮转 ──────────────────────────────────────────
        public int CurrentZhaoShiCardId { get; set; } = -1;
    }
}