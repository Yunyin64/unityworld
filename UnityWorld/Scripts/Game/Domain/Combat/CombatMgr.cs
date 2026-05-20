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

        public CombatResult RunCombat(Npc A, Npc B)
        {
            var combatScene = new CombatScene();
            combatScenes.Add(A.Id, combatScene);

            var participants = new[]
            {
                (A, CombatTeam.TeamA),
                (B, CombatTeam.TeamB)
            };
            combatScene.Init(12345,participants,600);
            return combatScene.Run();
        }
    }
}