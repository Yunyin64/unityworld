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
        public  float ShieldValue { get; private  set; }
        private float Hp { get; set; }
        private float Mp { get; set; }
        private float Sp { get ; set; }

        public int GetHp(){return (int)Hp;}
        public int GetCombatHpMax(){return GetHpMax();}
        public int GetMp(){return (int)Mp;}
        public int GetCombatMpMax(){return GetMpMax();}
        public int GetSp(){
            int sp = 0;
            foreach (var kv in CardDeck) sp += kv.GetSize();
            return  sp;}
        public int GetCombatSpMax(){return GetSpMax();}

        public CombatCard GetCardByIndex(int index)
        {
            return CardDeck[index];
        }

        public void ChangeShield(float val)
        {
            //可能要加入护盾上限值，待定
            ShieldValue += val;
            ShieldValue = Math.Clamp(ShieldValue, 0, float.MaxValue);
            Log($"护盾值变更：{val:+0.##;-0.##;0}，当前护盾={ShieldValue:0.##}");
        }
    }
}