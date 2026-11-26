namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        /// <summary>派发轮首事件，执行状态轮首回调并抽取灵元</summary>
        internal void BeginBackpackRound(bool opening = false)
        {
            DispatchRoundEvent("OnRoundStart");
            var ctx = new APIContext { Caster = this, Scene = Scene };
            ctx.Set("Round", Backpack.Round);
            ModifyHook("RoundStart", ctx);
            DoManaDraw(opening);
        }

        /// <summary>派发携带当前背包轮次的战斗事件</summary>
        internal void DispatchRoundEvent(string name)
        {
            var ctx = new APIContext { Caster = this, Scene = Scene };
            ctx.Set("Round", Backpack.Round);
            Scene.TriggerCombatEvent(name, ctx);
        }

        /// <summary>清除截至当前轮的装备修正和过期状态</summary>
        internal void ExpireRoundEffects()
        {
            _equipmentChanges.RemoveAll(effect => effect.LastRound <= Backpack.Round);
            foreach (var modifier in Modifiers.Where(effect => effect.LastRound <= Backpack.Round).ToArray())
                DoRemoveModifier(modifier);
        }
    }
}
