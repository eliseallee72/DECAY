using System;

namespace Decay
{
    /// <summary>
    /// Snapshot of where a resolved Fact occurred in the battle flow.
    /// This is historical context only; BattleState remains current authority.
    /// </summary>
    public readonly struct BattleFactContext : IEquatable<BattleFactContext>
    {
        public BattleFactContext(int gameNumber, int roundNumber, BattlePhase phase)
        {
            if (gameNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(gameNumber), gameNumber, "Game number must be at least 1.");
            }

            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber), roundNumber, "Round number must be at least 1.");
            }

            if (!Enum.IsDefined(typeof(BattlePhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Battle phase is not defined.");
            }

            GameNumber = gameNumber;
            RoundNumber = roundNumber;
            Phase = phase;
        }

        public int GameNumber { get; }
        public int RoundNumber { get; }
        public BattlePhase Phase { get; }
        public bool IsValid => GameNumber >= 1
            && RoundNumber >= 1
            && Enum.IsDefined(typeof(BattlePhase), Phase);

        public bool Equals(BattleFactContext other)
        {
            return GameNumber == other.GameNumber
                && RoundNumber == other.RoundNumber
                && Phase == other.Phase;
        }

        public override bool Equals(object obj) => obj is BattleFactContext other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + GameNumber;
                hash = (hash * 31) + RoundNumber;
                hash = (hash * 31) + (int)Phase;
                return hash;
            }
        }
        public static bool operator ==(BattleFactContext left, BattleFactContext right) => left.Equals(right);
        public static bool operator !=(BattleFactContext left, BattleFactContext right) => !left.Equals(right);

        public override string ToString() => $"Game {GameNumber}, Round {RoundNumber}, {Phase}";
    }
}
