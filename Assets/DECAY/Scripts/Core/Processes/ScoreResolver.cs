using System;

namespace Decay
{
    /// <summary>
    /// Pure SCORE rule calculation. It reads authoritative Board/BattleInventory state and returns immutable
    /// per-pair scoring decisions without mutating scores, dice, slots, phases, or presentation.
    /// </summary>
    public sealed class ScoreResolver
    {
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;

        public ScoreResolver(BoardState boardState, BattleInventoryState battleInventoryState)
        {
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
        }

        internal ScorePairResolution ResolvePair(SlotPairId pairId)
        {
            return new ScorePairResolution(
                pairId,
                ResolveSlot(pairId.EnemySlot),
                ResolveSlot(pairId.PlayerSlot));
        }

        private ScoreResolution? ResolveSlot(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (!slot.HasDice)
                return null;
            if (slot.Condition == SlotCondition.Broken)
                throw new InvalidOperationException($"Broken slot {slotId} cannot contain dice during SCORE.");
            if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState dice))
                throw new InvalidOperationException($"Board slot {slotId} contains untracked dice {slot.OccupantDiceId}.");
            if (dice.Owner != slotId.Side)
                throw new InvalidOperationException($"Dice {dice.InstanceId} ownership does not match slot {slotId}.");
            if (_battleInventoryState.IsInInventory(dice.InstanceId))
                throw new InvalidOperationException($"Dice {dice.InstanceId} cannot be on Board and in Battle Inventory simultaneously.");
            if (dice.IsDecayedForCurrentGame)
                throw new InvalidOperationException($"DECAYED dice {dice.InstanceId} cannot still occupy slot {slotId} during SCORE.");
            if (!dice.HasCurrentFace)
                throw new InvalidOperationException($"Dice {dice.InstanceId} has no current face for SCORE.");

            return new ScoreResolution(
                dice.InstanceId,
                slotId,
                slotId.Side,
                dice.CurrentFaceIndex,
                dice.ActiveRollValue,
                dice.GeneralScoreValue,
                dice.ActiveFaceScoreValue,
                checked(dice.GeneralScoreValue + dice.ActiveFaceScoreValue));
        }
    }

    internal readonly struct ScorePairResolution
    {
        internal ScorePairResolution(SlotPairId pairId, ScoreResolution? enemy, ScoreResolution? player)
        {
            PairId = pairId;
            Enemy = enemy;
            Player = player;
        }

        internal SlotPairId PairId { get; }
        internal ScoreResolution? Enemy { get; }
        internal ScoreResolution? Player { get; }
    }

    internal readonly struct ScoreResolution
    {
        internal ScoreResolution(
            DiceInstanceId diceId,
            SlotId slotId,
            Side side,
            int faceIndex,
            int rollValue,
            int generalScoreValue,
            int faceScoreValue,
            int scoreContribution)
        {
            DiceId = diceId;
            SlotId = slotId;
            Side = side;
            FaceIndex = faceIndex;
            RollValue = rollValue;
            GeneralScoreValue = generalScoreValue;
            FaceScoreValue = faceScoreValue;
            ScoreContribution = scoreContribution;
        }

        internal DiceInstanceId DiceId { get; }
        internal SlotId SlotId { get; }
        internal Side Side { get; }
        internal int FaceIndex { get; }
        internal int RollValue { get; }
        internal int GeneralScoreValue { get; }
        internal int FaceScoreValue { get; }
        internal int ScoreContribution { get; }
    }
}
