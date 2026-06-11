using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public class CombatMgr : IGameplayMgrBase,ISoulBase
    {
        public Dictionary<int,CombatScene> combatScenes = new();
        public string Name => "战斗管理器";

        public string Desc => "";

        public CombatMgr(int seed)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        public SoulData Soul {get;set;}
        public static CombatMgr Instance { get; private set; }

        public void Begin()
        {
             
        }

        public void End()
        {
             
        }

        public void Init()
        {
             
        }

        public void Log()
        {
             
        }

        public void Render(float dt)
        {
             
        }

        public void Tick(float deltaTime)
        {
             
        }

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
            var participants = teamA.Concat(teamB).ToArray();
            var combatScene = new CombatScene();
            var sceneId = participants[0].npc.Id;
            combatScenes[sceneId] = combatScene;

            combatScene.Init(12345, participants, 2000);
            return combatScene.Run();
        }
    }
}