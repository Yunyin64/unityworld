using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        private readonly Dictionary<int, Equip> _battleEquipment = new();
        private readonly Dictionary<int, Card> _equipmentSources = new();
        private readonly List<(int EquipmentId, string Stat, float Delta, int LastRound)> _equipmentChanges = new();

        internal Card FindBattleCard(int id) => CardData.AllCards.FirstOrDefault(card => card.Id == id)
            ?? _equipmentSources.GetValueOrDefault(id);

        private void SnapshotEquipment(Npc worldNpc)
        {
            // 已装备的父卡可以不在场上或候补；保留其数据而不额外创建场上卡。
            var sourceIds = CardData.AllCardIds.Concat(CardData.EquippedFaBao).ToHashSet();
            foreach (var card in worldNpc.GetAllCards())
            {
                if (!sourceIds.Contains(card.Id) || card.GetEquip() is not { } equip) continue;
                _battleEquipment.Add(card.Id, equip.Snapshot());
                _equipmentSources.Add(card.Id, card.CreateDataSnapshot());
            }
        }

        internal Equip ReadBattleEquipment(int id)
        {
            if (!_battleEquipment.TryGetValue(id, out var original)) return null;
            var result = original.Snapshot();
            double attack = original.Attack;
            double defense = original.Defend;
            double speed = original.Speed;
            foreach (var change in _equipmentChanges)
            {
                if (change.EquipmentId != id || change.LastRound < Backpack.Round) continue;
                switch (change.Stat)
                {
                    case "Attack": attack += change.Delta; break;
                    case "Defend": defense += change.Delta; break;
                    case "Speed": speed += change.Delta; break;
                }
            }

            result.Attack = (int)Math.Clamp(attack, 0, int.MaxValue);
            result.Defend = (int)Math.Clamp(defense, 0, int.MaxValue);
            result.Speed = (float)Math.Clamp(speed, 0, float.MaxValue);
            return result;
        }

        internal void ChangeEquipmentStat(CombatCard target, string stat, float delta, int rounds)
        {
            if (!float.IsFinite(delta) || delta == 0 || rounds < 0 ||
                stat is not ("Attack" or "Defend" or "Speed") ||
                (stat != "Speed" && delta != MathF.Truncate(delta))) return;

            var id = target.BaseData.MainKeyword == CardMainKeyword.ZhaoShi ? target.ParentCardId : target.Id;
            if (!_battleEquipment.ContainsKey(id)) return;
            _equipmentChanges.Add((id, stat, delta, GetLastEffectRound(rounds)));
            Scene.MarkPresentationEffect();
        }
    }
}
