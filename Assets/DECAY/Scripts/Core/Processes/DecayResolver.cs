using System;

namespace Decay
{
    /// <summary>
    /// Plain-C# DECAY rule calculator for one opposing SlotPairId. It reads authoritative Board and
    /// dice state and returns one simultaneous pair decision; it does not mutate state or consume saves.
    /// </summary>
    public sealed class DecayResolver
    {
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;

        public DecayResolver(BoardState boardState, BattleInventoryState battleInventoryState)
        {
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
        }

        internal DecayPairDecision ResolvePair(
            SlotPairId pairId,
            DecaySaveToken? enemyPendingSave,
            DecaySaveToken? playerPendingSave)
        {
            SlotState enemySlot = _boardState.GetSlot(pairId.EnemySlot);
            SlotState playerSlot = _boardState.GetSlot(pairId.PlayerSlot);

            bool markEnemyUnstableBefore = playerSlot.Condition == SlotCondition.Broken
                && enemySlot.Condition == SlotCondition.Unbroken;
            bool markPlayerUnstableBefore = enemySlot.Condition == SlotCondition.Broken
                && playerSlot.Condition == SlotCondition.Unbroken;

            SlotCondition enemyEffective = markEnemyUnstableBefore ? SlotCondition.Unstable : enemySlot.Condition;
            SlotCondition playerEffective = markPlayerUnstableBefore ? SlotCondition.Unstable : playerSlot.Condition;

            DecaySideSnapshot enemy = BuildSnapshot(pairId.EnemySlot, enemySlot, enemyEffective);
            DecaySideSnapshot player = BuildSnapshot(pairId.PlayerSlot, playerSlot, playerEffective);

            bool enemySource = enemy.HasDice && enemy.RollValue == BattleRules.MaximumRollValue;
            bool playerSource = player.HasDice && player.RollValue == BattleRules.MaximumRollValue;

            DecaySideDecision enemyDecision = BuildSideDecision(enemy, enemySource, playerSource, enemyPendingSave);
            DecaySideDecision playerDecision = BuildSideDecision(player, playerSource, enemySource, playerPendingSave);

            SlotCondition enemyAfter = DirectConditionAfter(enemyDecision);
            SlotCondition playerAfter = DirectConditionAfter(playerDecision);
            bool markEnemyUnstableAfter = playerAfter == SlotCondition.Broken && enemyAfter == SlotCondition.Unbroken;
            bool markPlayerUnstableAfter = enemyAfter == SlotCondition.Broken && playerAfter == SlotCondition.Unbroken;
            if (markEnemyUnstableAfter) enemyAfter = SlotCondition.Unstable;
            if (markPlayerUnstableAfter) playerAfter = SlotCondition.Unstable;

            bool createEnemySave = enemy.HasDice
                && enemyDecision.Outcome != DecayOutcome.Decayed
                && enemy.RollValue == BattleRules.MinimumRollValue;
            bool createPlayerSave = player.HasDice
                && playerDecision.Outcome != DecayOutcome.Decayed
                && player.RollValue == BattleRules.MinimumRollValue;

            return new DecayPairDecision(
                pairId,
                markEnemyUnstableBefore,
                markPlayerUnstableBefore,
                enemyDecision,
                playerDecision,
                markEnemyUnstableAfter,
                markPlayerUnstableAfter,
                createEnemySave,
                createPlayerSave,
                enemyAfter,
                playerAfter);
        }

        private DecaySideSnapshot BuildSnapshot(SlotId slotId, SlotState slot, SlotCondition effectiveCondition)
        {
            if (!slot.HasDice)
                return new DecaySideSnapshot(slotId, effectiveCondition, default, 0, false);

            if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState dice))
                throw new InvalidOperationException($"Board slot {slotId} contains untracked dice {slot.OccupantDiceId}.");
            if (dice.Owner != slotId.Side)
                throw new InvalidOperationException($"Dice {dice.InstanceId} ownership does not match slot {slotId}.");
            if (_battleInventoryState.IsInInventory(dice.InstanceId))
                throw new InvalidOperationException($"Dice {dice.InstanceId} cannot be on Board and in Battle Inventory simultaneously.");
            if (dice.IsDecayedForCurrentGame)
                throw new InvalidOperationException($"DECAYED dice {dice.InstanceId} cannot still occupy slot {slotId}.");
            if (!dice.HasCurrentFace)
                throw new InvalidOperationException($"Dice {dice.InstanceId} has no current face for DECAY.");

            return new DecaySideSnapshot(slotId, effectiveCondition, dice.InstanceId, dice.ActiveRollValue, true);
        }

        private static DecaySideDecision BuildSideDecision(
            DecaySideSnapshot snapshot,
            bool isWillDecay,
            bool isTargeted,
            DecaySaveToken? pendingSave)
        {
            bool threatened = isWillDecay || isTargeted;
            bool eligible = snapshot.HasDice && snapshot.EffectiveCondition == SlotCondition.Unbroken;
            DecayOutcome outcome = DecayOutcome.None;
            DecaySaveToken? saveUsed = null;

            if (snapshot.HasDice)
            {
                if (eligible && threatened)
                {
                    if (pendingSave.HasValue)
                    {
                        outcome = DecayOutcome.Saved;
                        saveUsed = pendingSave;
                    }
                    else
                    {
                        outcome = DecayOutcome.Decayed;
                    }
                }
                else
                {
                    outcome = DecayOutcome.Unaffected;
                }
            }

            return new DecaySideDecision(snapshot, isWillDecay, isTargeted, threatened, eligible, outcome, saveUsed);
        }

        private static SlotCondition DirectConditionAfter(DecaySideDecision decision)
        {
            if (decision.Outcome == DecayOutcome.Decayed) return SlotCondition.Broken;
            if (decision.Outcome == DecayOutcome.Saved) return SlotCondition.Unstable;
            return decision.Snapshot.EffectiveCondition;
        }
    }

    internal enum DecayOutcome
    {
        None,
        Unaffected,
        Saved,
        Decayed
    }

    internal readonly struct DecaySideSnapshot
    {
        internal DecaySideSnapshot(SlotId slotId, SlotCondition effectiveCondition, DiceInstanceId diceId, int rollValue, bool hasDice)
        {
            SlotId = slotId;
            EffectiveCondition = effectiveCondition;
            DiceId = diceId;
            RollValue = rollValue;
            HasDice = hasDice;
        }
        internal SlotId SlotId { get; }
        internal SlotCondition EffectiveCondition { get; }
        internal DiceInstanceId DiceId { get; }
        internal int RollValue { get; }
        internal bool HasDice { get; }
    }

    internal readonly struct DecaySideDecision
    {
        internal DecaySideDecision(
            DecaySideSnapshot snapshot,
            bool isWillDecay,
            bool isTargeted,
            bool isThreatened,
            bool isDecayEligible,
            DecayOutcome outcome,
            DecaySaveToken? saveUsed)
        {
            Snapshot = snapshot;
            IsWillDecay = isWillDecay;
            IsTargeted = isTargeted;
            IsThreatened = isThreatened;
            IsDecayEligible = isDecayEligible;
            Outcome = outcome;
            SaveUsed = saveUsed;
        }
        internal DecaySideSnapshot Snapshot { get; }
        internal bool IsWillDecay { get; }
        internal bool IsTargeted { get; }
        internal bool IsThreatened { get; }
        internal bool IsDecayEligible { get; }
        internal DecayOutcome Outcome { get; }
        internal DecaySaveToken? SaveUsed { get; }
    }

    internal sealed class DecayPairDecision
    {
        internal DecayPairDecision(
            SlotPairId pairId,
            bool markEnemyUnstableBefore,
            bool markPlayerUnstableBefore,
            DecaySideDecision enemy,
            DecaySideDecision player,
            bool markEnemyUnstableAfter,
            bool markPlayerUnstableAfter,
            bool createEnemySave,
            bool createPlayerSave,
            SlotCondition enemyConditionAfter,
            SlotCondition playerConditionAfter)
        {
            PairId = pairId;
            MarkEnemyUnstableBefore = markEnemyUnstableBefore;
            MarkPlayerUnstableBefore = markPlayerUnstableBefore;
            Enemy = enemy;
            Player = player;
            MarkEnemyUnstableAfter = markEnemyUnstableAfter;
            MarkPlayerUnstableAfter = markPlayerUnstableAfter;
            CreateEnemySave = createEnemySave;
            CreatePlayerSave = createPlayerSave;
            EnemyConditionAfter = enemyConditionAfter;
            PlayerConditionAfter = playerConditionAfter;
        }

        internal SlotPairId PairId { get; }
        internal bool MarkEnemyUnstableBefore { get; }
        internal bool MarkPlayerUnstableBefore { get; }
        internal DecaySideDecision Enemy { get; }
        internal DecaySideDecision Player { get; }
        internal bool MarkEnemyUnstableAfter { get; }
        internal bool MarkPlayerUnstableAfter { get; }
        internal bool CreateEnemySave { get; }
        internal bool CreatePlayerSave { get; }
        internal SlotCondition EnemyConditionAfter { get; }
        internal SlotCondition PlayerConditionAfter { get; }
    }
}
