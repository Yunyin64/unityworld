using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatScene
    {
        private Dictionary<int, CombatNpc> _combatants = new();
        private CombatResult _result;

        // 回调可以改变存活状态。每个结算步骤都重新枚举，保持按 NPC Id 执行的顺序。
        private IEnumerable<CombatNpc> OrderedCombatants => _combatants.Values.OrderBy(npc => npc.Id);
        private IEnumerable<CombatNpc> ActiveCombatants => OrderedCombatants.Where(npc => npc.IsActive);

        /// <summary>按大世界 NPC Id 查询本场的参战者。</summary>
        public IReadOnlyDictionary<int, CombatNpc> GetCombatants() => _combatants;

        /// <summary>读取本场结算结果。在结算前调用会抛出异常。</summary>
        public CombatResult GetResult() => _result ?? throw new InvalidOperationException("战斗尚未结束。");

        private void InitCombatants(IEnumerable<(Npc npc, CombatTeam team)> participants)
        {
            _combatants = new Dictionary<int, CombatNpc>();
            foreach (var (npc, team) in participants)
            {
                var combatNpc = CombatNpc.CreateCombatNpc(npc);
                combatNpc.Team = team;
                combatNpc.Scene = this;
                _combatants[combatNpc.Id] = combatNpc;
            }
        }

        private bool HasAtMostOneActiveTeam() =>
            _combatants.Values.Where(npc => npc.IsActive).Select(npc => npc.Team).Distinct().Count() <= 1;

        private void BuildCombatResult(CombatEndReason reason)
        {
            var survivors = _combatants.Values.Where(npc => npc.IsActive).ToList();
            var survivingTeams = survivors.Select(npc => npc.Team).Distinct().ToList();
            var winner = survivingTeams.Count == 1 ? survivingTeams[0] : CombatTeam.None;

            var results = _combatants.Values.Select(npc => new CombatantResult
            {
                NpcId = npc.Id,
                Team = npc.Team,
                FinalStatus = npc.Status,
                Consumption = npc.GetConsumptionResults()
            }).ToList();

            _result = CombatResult.CombatSceneResult(reason, winner, CurrentTick, results);
            var winnerNames = winner == CombatTeam.None ? "无" : string.Join("、", survivors.Select(npc => npc.GetName()));
            Log($"\n战斗结束！原因={reason}，胜者={winnerNames}，共{CurrentTick} Tick。");
        }

        /// <summary>汇总场上存活 NPC 的 Modifier 对指定属性的贡献。caller 是读取属性的 NPC 或卡牌。</summary>
        public float CollectModifierStat(object caller, string statId)
        {
            float sum = 0f;
            var hookName = "OnModifierStat" + statId;
            foreach (var npc in ActiveCombatants)
            {
                foreach (var mod in npc.GetAllModifiers())
                {
                    if (!mod.HasHook(hookName)) continue;
                    try
                    {
                        sum += (float)mod.CallLuaHook<double>(hookName, mod.env, caller);
                    }
                    catch (Exception ex)
                    {
                        LogMgr.Instance.Err("[CombatScene] CollectModifierStat '{0}' hook '{1}' 异常: {2}",
                            mod.DefineId, hookName, ex.Message);
                    }
                }
            }
            return sum;
        }
    }
}
