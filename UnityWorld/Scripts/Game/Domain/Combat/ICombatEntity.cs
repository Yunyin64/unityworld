namespace UnityWorld.Game.Domain.Combat
{
    public interface ICombatEntity
    {
        public void PreStart();
        public void Start();
        public void Tick();
        public void End();
        public void Cleanup();
        public void Log(string msg);
        public Dictionary<string,float> Ticks { get; set; }

        /// <summary>所属战斗场景引用，在 CombatScene.Init 中赋值</summary>
        public CombatScene Scene { get; set; }
    }
}