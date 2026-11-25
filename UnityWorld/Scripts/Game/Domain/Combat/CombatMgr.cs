using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public class CombatMgr : IGameplayMgrBase,ISoulBase
    {
        /// <summary>按首位参战者编号索引的战斗场景</summary>
        public Dictionary<int,CombatScene> combatScenes = new();
        /// <summary>串行执行世界战斗和回写的锁</summary>
        private readonly object _worldCombatGate = new();
        /// <summary>战斗管理器名称</summary>
        public string Name => "战斗管理器";

        /// <summary>管理器说明，当前为空</summary>
        public string Desc => "";

        /// <summary>按种子初始化管理器数据并登记实例</summary>
        public CombatMgr(int seed)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        /// <summary>管理器的基础实体数据</summary>
        public SoulData Soul {get;set;}
        /// <summary>当前战斗管理器</summary>
        public static CombatMgr Instance { get; private set; }

        /// <summary>保留世界启动回调</summary>
        public void Begin()
        {
             
        }

        /// <summary>保留世界结束回调</summary>
        public void End()
        {
             
        }

        /// <summary>保留管理器初始化入口</summary>
        public void Init()
        {
             
        }

        /// <summary>保留战斗管理日志入口</summary>
        public void Log()
        {
             
        }

        /// <summary>保留渲染回调</summary>
        public void Render(float dt)
        {
             
        }

        /// <summary>保留时步回调</summary>
        public void Tick(float deltaTime)
        {
             
        }

        /// <summary>保留帧更新回调</summary>
        public void Update()
        {
             
        }

        /// <summary>
        /// 1v1 便捷入口
        /// </summary>
        public CombatResult RunCombat(Npc A, Npc B)
        {
            return RunCombat(
                new[] { (A, CombatTeam.TeamA) },
                new[] { (B, CombatTeam.TeamB) });
        }

        /// <summary>
        /// 1vN 便捷入口：单个 Npc 对抗多个敌人
        /// </summary>
        public CombatResult RunCombat(Npc solo, IEnumerable<Npc> enemies)
        {
            var teamA = new[] { (solo, CombatTeam.TeamA) };
            var teamB = enemies.Select(e => (e, CombatTeam.TeamB));
            return RunCombat(teamA, teamB);
        }

        /// <summary>
        /// 通用多人入口：传入任意数量 (Npc, CombatTeam) 组合
        /// </summary>
        public CombatResult RunCombat(IEnumerable<(Npc npc, CombatTeam team)> teamA, IEnumerable<(Npc npc, CombatTeam team)> teamB)
        {
            lock (_worldCombatGate)
            {
                var participants = teamA.Concat(teamB).ToArray();
                var combatScene = new CombatScene();
                var sceneId = participants[0].npc.Id;
                combatScenes[sceneId] = combatScene;

                combatScene.Init(12345, participants, 2000);
                var result = combatScene.Run();
                result.CommitToWorld(participants);
                return result;
            }
        }
    }
}
