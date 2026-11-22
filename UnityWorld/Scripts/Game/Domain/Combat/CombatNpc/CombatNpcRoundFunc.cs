namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        internal void BeginBackpackRound(bool opening = false)
        {
            DispatchRoundEvent("OnRoundStart");
            PowerArtifacts();
            DoManaDraw(opening);
        }

        internal void DispatchRoundEvent(string name)
        {
            var ctx = new APIContext { Caster = this, Scene = Scene };
            ctx.Set("Round", Backpack.Round);
            Scene.TriggerCombatEvent(name, ctx);
        }

        internal void ExpireRoundEffects()
        {
            _equipmentChanges.RemoveAll(effect => effect.LastRound <= Backpack.Round);
            foreach (var modifier in Modifiers.Where(effect => effect.LastRound <= Backpack.Round).ToArray())
                DoRemoveModifier(modifier);
        }
    }
}
