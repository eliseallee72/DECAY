namespace Decay
{
    public sealed class PhaseChangeRequest
    {
        public PhaseChangeRequest(BattlePhase requestedPhase)
        {
            RequestedPhase = requestedPhase;
        }

        public BattlePhase RequestedPhase { get; }
    }
}
