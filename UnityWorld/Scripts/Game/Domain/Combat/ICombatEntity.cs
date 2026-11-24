namespace UnityWorld.Game.Domain.Combat
{
    public interface ICombatEntity
    {
        /// <summary>准备战斗数据和开战前状态</summary>
        public void PreStart();
        /// <summary>执行实体的开战逻辑</summary>
        public void Start();
        /// <summary>执行实体的单次战斗时步</summary>
        public void Tick();
        /// <summary>执行实体的战斗结束逻辑</summary>
        public void End();
        /// <summary>清理实体持有的战斗资源</summary>
        public void Cleanup();
        /// <summary>记录实体在战斗中的消息</summary>
        public void Log(string msg);
        /// <summary>按名称记录的战斗时步计数</summary>
        public Dictionary<string,float> Ticks { get; set; }

        /// <summary>所属战斗场景引用，在 CombatScene.Init 中赋值</summary>
        public CombatScene Scene { get; set; }
    }
}