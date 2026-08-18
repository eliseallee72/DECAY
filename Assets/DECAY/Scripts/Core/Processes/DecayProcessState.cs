using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Ephemeral authority for one DECAY pass. It owns current pair progression and pending WILLSAVE
    /// queues while the process is active; no duplicate save state is stored on DiceRuntimeState or views.
    /// </summary>
    internal sealed class DecayProcessState
    {
        private readonly Queue<DecaySaveToken> _enemySaves = new Queue<DecaySaveToken>();
        private readonly Queue<DecaySaveToken> _playerSaves = new Queue<DecaySaveToken>();
        private int _currentPairNumber = BattleRules.FirstSlotNumber;

        internal DecayProcessState(BattleFactContext context)
        {
            if (context.Phase != BattlePhase.DecayProcess)
                throw new ArgumentException("DecayProcessState requires DecayProcess context.", nameof(context));
            Context = context;
        }

        internal BattleFactContext Context { get; }
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

        internal void AddSave(DecaySaveToken token)
        {
            GetQueue(token.Side).Enqueue(token);
        }

        internal DecaySaveToken ConsumeNextSave(Side side, DecaySaveToken expected)
        {
            Queue<DecaySaveToken> queue = GetQueue(side);
            if (queue.Count == 0) throw new InvalidOperationException($"{side} has no pending WILLSAVE to consume.");
            DecaySaveToken actual = queue.Peek();
            if (actual != expected)
                throw new InvalidOperationException("DECAY save consumption order changed after pair resolution was approved.");
            return queue.Dequeue();
        }

        internal void AdvancePair()
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
