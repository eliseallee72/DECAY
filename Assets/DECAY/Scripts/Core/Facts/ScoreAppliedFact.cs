namespace Decay
{
    public sealed class ScoreAppliedFact : BattleFact
    {
        internal ScoreAppliedFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            SlotId slotId,
            Side side,
            int faceIndex,
            int rollValue,
            int generalScoreValue,
            int faceScoreValue,
            int appliedScore,
            int resultingRoundScore,
            int resultingTotalScore)
        {
            Context = context;
            DiceId = diceId;
            SlotId = slotId;
            Side = side;
            FaceIndex = faceIndex;
            RollValue = rollValue;
            GeneralScoreValue = generalScoreValue;
            FaceScoreValue = faceScoreValue;
            AppliedScore = appliedScore;
            ResultingRoundScore = resultingRoundScore;
            ResultingTotalScore = resultingTotalScore;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public SlotId SlotId { get; }
        public Side Side { get; }
        public int FaceIndex { get; }
        public int RollValue { get; }
        public int GeneralScoreValue { get; }
        public int FaceScoreValue { get; }
        public int AppliedScore { get; }
        public int ResultingRoundScore { get; }
        public int ResultingTotalScore { get; }
    }
}
