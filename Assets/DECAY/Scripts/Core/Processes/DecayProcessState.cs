using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Ephemeral sequence state for one DECAY calculation. It owns current pair progression and pending
    /// WILLSAVE queues for both committed execution and read-only preview; it is never stored on dice or views.
    /// </summary>
    internal sealed class DecayProcessState
    {
        private readonly Queue<DecaySaveToken> _enemySaves = new Queue<DecaySaveToken>();
        private readonly Queue<DecaySaveToken> _playerSaves = new Queue<DecaySaveToken>();
        private int _currentPairNumber = BattleRules.FirstSlotNumber;

        internal bool IsComplete { get; private set; }
        internal SlotPairId CurrentPairId
        {
            get
            {
                if (IsComplete) throw new InvalidOperationException("DECAY process has already resolved every pair.");
                return new SlotPairId(_currentPairNumber);
            }
        }

        internal int PendingSaveCount(Side side) => GetQueue(side).Count;

        internal bool TryPeekNextSave(Side side, out DecaySaveToken token)
        {
            Queue<DecaySaveToken> queue = GetQueue(side);
            if (queue.Count > 0)
            {
                token = queue.Peek();
                return true;
            }
            token = default;
            return false;
        }

        internal void ApplyResolvedDecision(DecayPairDecision decision)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (IsComplete) throw new InvalidOperationException("DECAY process is already complete.");
            if (decision.PairId != CurrentPairId)
                throw new InvalidOperationException($"Expected DECAY pair {CurrentPairId}, received {decision.PairId}.");

            ApplySideSaveUse(decision.Enemy);
            ApplySideSaveUse(decision.Player);

            if (decision.CreateEnemySave)
            {
                AddSave(new DecaySaveToken(decision.Enemy.Snapshot.DiceId, decision.Enemy.Snapshot.SlotId));
            }

            if (decision.CreatePlayerSave)
            {
                AddSave(new DecaySaveToken(decision.Player.Snapshot.DiceId, decision.Player.Snapshot.SlotId));
            }

            AdvancePair();
        }

        private void ApplySideSaveUse(DecaySideDecision decision)
        {
            if (!decision.SaveUsed.HasValue)
            {
                return;
            }

            ConsumeNextSave(decision.Snapshot.SlotId.Side, decision.SaveUsed.Value);
        }

        private void AddSave(DecaySaveToken token)
        {
            GetQueue(token.Side).Enqueue(token);
        }

        private DecaySaveToken ConsumeNextSave(Side side, DecaySaveToken expected)
        {
            Queue<DecaySaveToken> queue = GetQueue(side);
            if (queue.Count == 0) throw new InvalidOperationException($"{side} has no pending WILLSAVE to consume.");
            DecaySaveToken actual = queue.Peek();
            if (actual != expected)
                throw new InvalidOperationException("DECAY save consumption order changed after pair resolution was approved.");
            return queue.Dequeue();
        }

        private void AdvancePair()
        {
            if (IsComplete) throw new InvalidOperationException("DECAY process is already complete.");
            if (_currentPairNumber >= BattleRules.LastSlotNumber)
            {
                IsComplete = true;
                return;
            }
            _currentPairNumber++;
        }

        private Queue<DecaySaveToken> GetQueue(Side side)
        {
            if (!Enum.IsDefined(typeof(Side), side))
                throw new ArgumentOutOfRangeException(nameof(side), side, "Side must be Enemy or Player.");
            return side == Side.Enemy ? _enemySaves : _playerSaves;
        }
    }
}
