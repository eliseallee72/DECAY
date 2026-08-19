using System.Collections.Generic;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class DecayPreviewTests
    {
        [Test]
        public void Preview_UsesAuthoritativeSaveSequenceWithoutMutatingBattleState()
        {
            var f = new PreviewFixture();
            DiceRuntimeState saver = f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState target = f.Place(Side.Player, 2, 5);
            int historyCountBefore = f.History.Count;

            DecayPreviewResult preview = f.Executor.ResolvePreview();

            DecayPreviewSide source = preview.Pairs[0].Player;
            DecayPreviewSide saved = preview.Pairs[1].Player;
            Assert.That(source.WillCreateSave, Is.True);
            Assert.That(saved.Outcome, Is.EqualTo(DecayOutcome.Saved));
            Assert.That(saved.HasSaveSource, Is.True);
            Assert.That(saved.SaveSourceDiceId, Is.EqualTo(saver.InstanceId));
            Assert.That(saved.SaveSourceSlotId, Is.EqualTo(new SlotId(Side.Player, 1)));
            Assert.That(target.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Unbroken));
            Assert.That(f.History.Count, Is.EqualTo(historyCountBefore));
        }

        [Test]
        public void Preview_OneOpposingSixDoesNotPredictSaveCreation()
        {
            var f = new PreviewFixture();
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState one = f.Place(Side.Player, 2, 1);

            DecayPreviewSide side = f.Executor.ResolvePreview().Pairs[1].Player;

            Assert.That(side.DiceId, Is.EqualTo(one.InstanceId));
            Assert.That(side.IsTargeted, Is.True);
            Assert.That(side.Outcome, Is.EqualTo(DecayOutcome.Decayed));
            Assert.That(side.WillCreateSave, Is.False);
        }

        [Test]
        public void Preview_SixVersusSixCarriesTargetingSourceForBothSides()
        {
            var f = new PreviewFixture();
            DiceRuntimeState enemy = f.Place(Side.Enemy, 3, 6);
            DiceRuntimeState player = f.Place(Side.Player, 3, 6);

            DecayPreviewPair pair = f.Executor.ResolvePreview().Pairs[2];

            Assert.That(pair.Enemy.IsWillDecay, Is.True);
            Assert.That(pair.Enemy.IsTargeted, Is.True);
            Assert.That(pair.Enemy.TargetingDiceId, Is.EqualTo(player.InstanceId));
            Assert.That(pair.Player.IsWillDecay, Is.True);
            Assert.That(pair.Player.IsTargeted, Is.True);
            Assert.That(pair.Player.TargetingDiceId, Is.EqualTo(enemy.InstanceId));
        }

        [Test]
        public void CommittedDecay_UsesSameDecisionInformationAsPreview()
        {
            var f = new PreviewFixture();
            f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            f.Place(Side.Player, 2, 5);
            f.Place(Side.Enemy, 4, 6);
            f.Place(Side.Player, 4, 6);

            DecayPreviewResult preview = f.Executor.ResolvePreview();
            new AdvancePhaseCommand(f.State, BattlePhase.DecayProcess).Execute();
            DecayExecutionResult committed = f.Executor.ExecuteDecay();

            for (int i = 0; i < BattleRules.SlotsPerSide; i++)
            {
                AssertEquivalent(preview.Pairs[i].Enemy, committed.PairResolutions[i].Enemy);
                AssertEquivalent(preview.Pairs[i].Player, committed.PairResolutions[i].Player);
            }
        }

        private static void AssertEquivalent(DecayPreviewSide preview, DecaySideResolution committed)
        {
            Assert.That(committed.SlotId, Is.EqualTo(preview.SlotId));
            Assert.That(committed.WasWillDecay, Is.EqualTo(preview.IsWillDecay));
            Assert.That(committed.WasTargeted, Is.EqualTo(preview.IsTargeted));
            Assert.That(committed.TargetingDiceId, Is.EqualTo(preview.TargetingDiceId));
            Assert.That(committed.TargetingSlotId, Is.EqualTo(preview.TargetingSlotId));
            Assert.That(committed.WasDecayEligible, Is.EqualTo(preview.IsDecayEligible));
            Assert.That(committed.Outcome, Is.EqualTo(preview.Outcome));
            Assert.That(committed.CreatedSave, Is.EqualTo(preview.WillCreateSave));
            Assert.That(committed.UsedSave, Is.EqualTo(preview.HasSaveSource));
            Assert.That(committed.SaveSourceDiceId, Is.EqualTo(preview.SaveSourceDiceId));
            Assert.That(committed.SaveSourceSlotId, Is.EqualTo(preview.SaveSourceSlotId));
        }

        private sealed class PreviewFixture
        {
            private readonly Dictionary<(Side, int), DiceRuntimeState> _dice = new Dictionary<(Side, int), DiceRuntimeState>();

            internal PreviewFixture()
            {
                State = DiceTestFactory.CreateBattleState();
                Board = new BoardState();
                var dice = new List<DiceRuntimeState>();
                for (int i = 1; i <= 6; i++)
                {
                    DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(i, $"dice.preview_enemy_{i}");
                    DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(100 + i, 1000 + i, $"dice.preview_player_{i}");
                    _dice.Add((Side.Enemy, i), enemy);
                    _dice.Add((Side.Player, i), player);
                    dice.Add(enemy);
                    dice.Add(player);
                }

                Inventory = new BattleInventoryState(10, dice);
                History = new BattleHistory();
                Executor = new DecayExecutor(State, Board, Inventory, History);
                new AdvancePhaseCommand(State, BattlePhase.Rolling).Execute();
                new AdvancePhaseCommand(State, BattlePhase.EnemyReposition).Execute();
                new AdvancePhaseCommand(State, BattlePhase.PlayerReposition).Execute();
            }

            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal BattleHistory History { get; }
            internal DecayExecutor Executor { get; }

            internal DiceRuntimeState Place(Side side, int slotNumber, int rollValue)
            {
                DiceRuntimeState dice = _dice[(side, slotNumber)];
                Inventory.RemoveFromInventory(dice.InstanceId);
                Board.PlaceDice(new SlotId(side, slotNumber), dice.InstanceId);
                dice.SetCurrentFace(rollValue);
                return dice;
            }
        }
    }
}
