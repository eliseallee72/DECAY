using System;

namespace Decay
{
    internal sealed class ClearDiceCurrentFaceCommand
    {
        private readonly DiceRuntimeState _diceState;
        private bool _wasExecuted;

        internal ClearDiceCurrentFaceCommand(DiceRuntimeState diceState)
        {
            _diceState = diceState ?? throw new ArgumentNullException(nameof(diceState));
        }

        internal void Execute()
        {
            if (_wasExecuted) throw new InvalidOperationException("A ClearDiceCurrentFaceCommand can only execute once.");
            if (_diceState.IsDecayedForCurrentGame)
            {
                if (_diceState.HasCurrentFace)
                    throw new InvalidOperationException($"DECAYED dice {_diceState.InstanceId} must not retain a current face.");
            }
            else if (_diceState.HasCurrentFace)
            {
                _diceState.ClearCurrentFace();
            }

            _wasExecuted = true;
        }
    }
}
