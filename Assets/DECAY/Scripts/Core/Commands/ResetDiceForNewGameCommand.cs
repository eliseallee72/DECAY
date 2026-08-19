using System;

namespace Decay
{
    public sealed class ResetDiceForNewGameCommand
    {
        private readonly BattleState _battleState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly DiceRuntimeState _diceState;
        private readonly DiceRuntimeSeed _resetSeed;
        private bool _wasExecuted;

        internal ResetDiceForNewGameCommand(
            BattleState battleState,
            BattleInventoryState battleInventoryState,
            DiceRuntimeState diceState,
            DiceRuntimeSeed resetSeed)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _diceState = diceState ?? throw new ArgumentNullException(nameof(diceState));
            _resetSeed = resetSeed ?? throw new ArgumentNullException(nameof(resetSeed));
        }

        internal DiceResetForNewGameFact Execute()
        {
            if (_wasExecuted) throw new InvalidOperationException("A ResetDiceForNewGameCommand can only execute once.");
            if (_battleState.CurrentPhase != BattlePhase.GameEnd)
                throw new InvalidOperationException("DECAYED dice may only repopulate during GameEnd.");
            if (!_diceState.IsDecayedForCurrentGame)
                throw new InvalidOperationException($"Dice {_diceState.InstanceId} is not DECAYED and must not be reset from its source.");
            if (_battleInventoryState.IsInInventory(_diceState.InstanceId))
                throw new InvalidOperationException($"DECAYED dice {_diceState.InstanceId} cannot already be in Battle Inventory.");

            _diceState.ResetFromSeed(_resetSeed);
            _battleInventoryState.ReturnToInventory(_diceState.InstanceId);
            _wasExecuted = true;
            return new DiceResetForNewGameFact(
                _battleState.CurrentFactContext,
                _diceState.InstanceId,
                _diceState.Owner,
                _diceState.DefinitionId);
        }
    }
}
