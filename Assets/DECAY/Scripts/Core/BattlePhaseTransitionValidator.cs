using System;

namespace Decay
{
    public sealed class BattlePhaseTransitionValidator
    {
        public bool IsTransitionAllowed(BattlePhase currentPhase, BattlePhase requestedPhase)
        {
            if (!Enum.IsDefined(typeof(BattlePhase), currentPhase)
                || !Enum.IsDefined(typeof(BattlePhase), requestedPhase))
            {
                return false;
            }

            switch (currentPhase)
            {
                case BattlePhase.Setup:
                    return requestedPhase == BattlePhase.Rolling;
                case BattlePhase.Rolling:
                    return requestedPhase == BattlePhase.EnemyReposition;
                case BattlePhase.EnemyReposition:
                    return requestedPhase == BattlePhase.PlayerReposition;
                case BattlePhase.PlayerReposition:
                    return requestedPhase == BattlePhase.DecayProcess;
                case BattlePhase.DecayProcess:
                    return requestedPhase == BattlePhase.ScoreProcess;
                case BattlePhase.ScoreProcess:
                    return requestedPhase == BattlePhase.RoundEnd;
                case BattlePhase.RoundEnd:
                    return requestedPhase == BattlePhase.Setup || requestedPhase == BattlePhase.GameEnd;
                case BattlePhase.GameEnd:
                    return requestedPhase == BattlePhase.Setup || requestedPhase == BattlePhase.BattleEnd;
                case BattlePhase.BattleEnd:
                    return false;
                default:
                    return false;
            }
        }

        public void RequireAllowedTransition(BattlePhase currentPhase, BattlePhase requestedPhase)
        {
            if (!IsTransitionAllowed(currentPhase, requestedPhase))
            {
                throw new InvalidOperationException(
                    $"Battle phase transition from {currentPhase} to {requestedPhase} is not allowed.");
            }
        }
    }
}
