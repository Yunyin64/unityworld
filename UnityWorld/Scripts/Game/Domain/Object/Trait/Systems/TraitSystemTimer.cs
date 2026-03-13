
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait 计时系统：负责推进每个 Trait 实例的 ElapsedTime，
    /// 并在 Trait 配置了持续时间时自动到期移除
    /// </summary>
    public class TraitSystemTimer : TraitSystemBase
    {

        public override void OnTick(Trait trait, float deltaTime)
        {
            trait.TickTime(deltaTime);

            // 如果 define 配置了持续时间，到期后自动移除
        }
    }
}
