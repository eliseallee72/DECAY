using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class DecayProcessTests
    {
        [Test]
        public void EmptyBoard_ResolvesAllSixPairsWithoutFacts()
        {
            var f = new DecayFixture();
            DecayExecutionResult result = f.Execute();

            Assert.That(result.PairResolutions.Count, Is.EqualTo(6));
            Assert.That(result.Facts.Count, Is.Zero);
            Assert.That(f.Executor.EvaluateCompletion(result), Is.EqualTo(BattleFlowDenialReason.None));
        }

        [Test]
        public void NoOneOrSix_LeavesOccupiedSlotsAndDiceUnchanged()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemy = f.Place(Side.Enemy, 2, 4);
            DiceRuntimeState player = f.Place(Side.Player, 2, 5);

            f.Execute();

            Assert.That(f.Board.GetSlot(new SlotId(Side.Enemy, 2)).OccupantDiceId, Is.EqualTo(enemy.InstanceId));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).OccupantDiceId, Is.EqualTo(player.InstanceId));
            Assert.That(enemy.IsDecayedForCurrentGame, Is.False);
            Assert.That(player.IsDecayedForCurrentGame, Is.False);
        }

        [Test]
        public void SixOpposingFive_DecaysBothDiceAndBreaksBothSlots()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemy = f.Place(Side.Enemy, 3, 6);
            DiceRuntimeState player = f.Place(Side.Player, 3, 5);

            f.Execute();

            AssertDecayed(f, enemy, new SlotId(Side.Enemy, 3));
            AssertDecayed(f, player, new SlotId(Side.Player, 3));
        }

        [Test]
        public void SixOpposingEmpty_DecaysItselfAndMakesEmptyOpposingSlotUnstable()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemy = f.Place(Side.Enemy, 2, 6);

            f.Execute();

            AssertDecayed(f, enemy, new SlotId(Side.Enemy, 2));
            SlotState playerSlot = f.Board.GetSlot(new SlotId(Side.Player, 2));
            Assert.That(playerSlot.HasDice, Is.False);
            Assert.That(playerSlot.Condition, Is.EqualTo(SlotCondition.Unstable));
        }

        [Test]
        public void EarlierOne_CreatesWillSaveAndProtectsFirstLaterThreat()
        {
            var f = new DecayFixture();
            DiceRuntimeState saver = f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState target = f.Place(Side.Player, 2, 5);

            f.Execute();

            Assert.That(target.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));
            SaveSpentFact spent = f.Facts<SaveSpentFact>().Single();
            Assert.That(spent.SourceDiceId, Is.EqualTo(saver.InstanceId));
            Assert.That(spent.TargetDiceId, Is.EqualTo(target.InstanceId));
        }

        [Test]
        public void OneOpposingSixWithoutEarlierSave_DecaysAndCreatesNoSave()
        {
            var f = new DecayFixture();
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState one = f.Place(Side.Player, 2, 1);

            f.Execute();

            Assert.That(one.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Facts<SaveCreatedFact>().Any(x => x.SourceDiceId == one.InstanceId), Is.False);
        }

        [Test]
        public void LaterOne_CannotRetroactivelySaveEarlierDecay()
        {
            var f = new DecayFixture();
            f.Place(Side.Enemy, 1, 6);
            DiceRuntimeState earlierTarget = f.Place(Side.Player, 1, 5);
            DiceRuntimeState laterOne = f.Place(Side.Player, 2, 1);

            f.Execute();

            Assert.That(earlierTarget.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Facts<SaveCreatedFact>().Single().SourceDiceId, Is.EqualTo(laterOne.InstanceId));
        }

        [Test]
        public void SixVersusSixWithoutSaves_DecaysBoth()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemy = f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState player = f.Place(Side.Player, 2, 6);

            f.Execute();

            Assert.That(enemy.IsDecayedForCurrentGame, Is.True);
            Assert.That(player.IsDecayedForCurrentGame, Is.True);
        }

        [Test]
        public void SixVersusSixWithPlayerSave_SavesPlayerSixOnly()
        {
            var f = new DecayFixture();
            f.Place(Side.Player, 1, 1);
            DiceRuntimeState enemySix = f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState playerSix = f.Place(Side.Player, 2, 6);

            f.Execute();

            Assert.That(enemySix.IsDecayedForCurrentGame, Is.True);
            Assert.That(playerSix.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));
        }

        [Test]
        public void SixVersusSixWithBothSidesSaved_SavesBothIndependently()
        {
            var f = new DecayFixture();
            f.Place(Side.Enemy, 1, 1);
            f.Place(Side.Player, 1, 1);
            DiceRuntimeState enemySix = f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState playerSix = f.Place(Side.Player, 2, 6);

            f.Execute();

            Assert.That(enemySix.IsDecayedForCurrentGame, Is.False);
            Assert.That(playerSix.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Facts<SaveSpentFact>().Count(), Is.EqualTo(2));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Enemy, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));
        }

        [Test]
        public void OneSave_ProtectsOnlyFirstEligibleLaterDecay()
        {
            var f = new DecayFixture();
            f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState firstTarget = f.Place(Side.Player, 2, 5);
            f.Place(Side.Enemy, 3, 6);
            DiceRuntimeState secondTarget = f.Place(Side.Player, 3, 5);

            f.Execute();

            Assert.That(firstTarget.IsDecayedForCurrentGame, Is.False);
            Assert.That(secondTarget.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Facts<SaveSpentFact>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void TwoSaves_AreConsumedInActivationOrderAcrossLaterThreats()
        {
            var f = new DecayFixture();
            DiceRuntimeState saverOne = f.Place(Side.Player, 1, 1);
            DiceRuntimeState saverTwo = f.Place(Side.Player, 2, 1);
            f.Place(Side.Enemy, 3, 6);
            DiceRuntimeState targetThree = f.Place(Side.Player, 3, 5);
            f.Place(Side.Enemy, 4, 6);
            DiceRuntimeState targetFour = f.Place(Side.Player, 4, 5);

            f.Execute();

            SaveSpentFact[] spent = f.Facts<SaveSpentFact>().Where(x => x.Side == Side.Player).ToArray();
            Assert.That(spent.Length, Is.EqualTo(2));
            Assert.That(spent[0].SourceDiceId, Is.EqualTo(saverOne.InstanceId));
            Assert.That(spent[0].TargetDiceId, Is.EqualTo(targetThree.InstanceId));
            Assert.That(spent[1].SourceDiceId, Is.EqualTo(saverTwo.InstanceId));
            Assert.That(spent[1].TargetDiceId, Is.EqualTo(targetFour.InstanceId));
        }

        [Test]
        public void Saves_AreSideSpecificAndNeverCrossToOpposingSide()
        {
            var f = new DecayFixture();
            f.Place(Side.Enemy, 1, 1);
            DiceRuntimeState enemyTarget = f.Place(Side.Enemy, 2, 5);
            DiceRuntimeState playerSix = f.Place(Side.Player, 2, 6);

            f.Execute();

            Assert.That(enemyTarget.IsDecayedForCurrentGame, Is.False);
            Assert.That(playerSix.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Facts<SaveSpentFact>().Single().Side, Is.EqualTo(Side.Enemy));
        }

        [Test]
        public void SavedOne_SurvivesThenCreatesANewSaveForLaterSlot()
        {
            var f = new DecayFixture();
            DiceRuntimeState firstSaver = f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState savedOne = f.Place(Side.Player, 2, 1);
            f.Place(Side.Enemy, 3, 6);
            DiceRuntimeState laterTarget = f.Place(Side.Player, 3, 5);

            f.Execute();

            SaveSpentFact[] spent = f.Facts<SaveSpentFact>().Where(x => x.Side == Side.Player).ToArray();
            Assert.That(spent.Length, Is.EqualTo(2));
            Assert.That(spent[0].SourceDiceId, Is.EqualTo(firstSaver.InstanceId));
            Assert.That(spent[0].TargetDiceId, Is.EqualTo(savedOne.InstanceId));
            Assert.That(spent[1].SourceDiceId, Is.EqualTo(savedOne.InstanceId));
            Assert.That(spent[1].TargetDiceId, Is.EqualTo(laterTarget.InstanceId));
            Assert.That(savedOne.IsDecayedForCurrentGame, Is.False);
            Assert.That(laterTarget.IsDecayedForCurrentGame, Is.False);
        }

        [Test]
        public void UnbrokenSlotOpposingBrokenSlot_BecomesUnstableBeforeDecayAndProtectsItsSix()
        {
            var f = new DecayFixture();
            f.SetBroken(Side.Enemy, 2);
            DiceRuntimeState playerSix = f.Place(Side.Player, 2, 6);

            f.Execute();

            Assert.That(playerSix.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));
        }

        [Test]
        public void SixInAlreadyUnstableSlot_StillTargetsStableOpposingDiceButDoesNotDecayItself()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemySix = f.Place(Side.Enemy, 2, 6);
            f.SetUnstable(Side.Enemy, 2);
            DiceRuntimeState playerTarget = f.Place(Side.Player, 2, 5);

            f.Execute();

            Assert.That(enemySix.IsDecayedForCurrentGame, Is.False);
            Assert.That(playerTarget.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Enemy, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));
        }

        [Test]
        public void DecayedDice_RemainsTrackedButNotInventoryMemberAndClearsCurrentFace()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemySix = f.Place(Side.Enemy, 2, 6);

            f.Execute();

            Assert.That(f.Inventory.ContainsDice(enemySix.InstanceId), Is.True);
            Assert.That(f.Inventory.IsInInventory(enemySix.InstanceId), Is.False);
            Assert.That(enemySix.IsDecayedForCurrentGame, Is.True);
            Assert.That(enemySix.HasCurrentFace, Is.False);
        }

        [Test]
        public void SavedDice_RemainsOnBoardAndPreservesCurrentFaceForScore()
        {
            var f = new DecayFixture();
            f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState target = f.Place(Side.Player, 2, 5);

            f.Execute();

            Assert.That(target.HasCurrentFace, Is.True);
            Assert.That(target.ActiveRollValue, Is.EqualTo(5));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).OccupantDiceId, Is.EqualTo(target.InstanceId));
            Assert.That(f.Inventory.IsInInventory(target.InstanceId), Is.False);
        }

        [Test]
        public void Facts_PreserveSaveCausalityAndPairOrder()
        {
            var f = new DecayFixture();
            DiceRuntimeState saver = f.Place(Side.Player, 1, 1);
            f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState target = f.Place(Side.Player, 2, 5);

            f.Execute();

            SaveCreatedFact created = f.Facts<SaveCreatedFact>().Single();
            SaveSpentFact spent = f.Facts<SaveSpentFact>().Single();
            DiceSavedFact saved = f.Facts<DiceSavedFact>().Single();
            Assert.That(created.SourceDiceId, Is.EqualTo(saver.InstanceId));
            Assert.That(spent.SourceDiceId, Is.EqualTo(saver.InstanceId));
            Assert.That(spent.TargetDiceId, Is.EqualTo(target.InstanceId));
            Assert.That(saved.SaviorDiceId, Is.EqualTo(saver.InstanceId));
            Assert.That(created.SequenceNumber, Is.LessThan(spent.SequenceNumber));
            Assert.That(spent.SequenceNumber, Is.LessThan(saved.SequenceNumber));
        }

        [Test]
        public void DiceDecayedFact_DistinguishesWillDecayFromTargetedStatus()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemySix = f.Place(Side.Enemy, 2, 6);
            DiceRuntimeState playerFive = f.Place(Side.Player, 2, 5);

            f.Execute();

            DiceDecayedFact sourceFact = f.Facts<DiceDecayedFact>().Single(x => x.DiceId == enemySix.InstanceId);
            DiceDecayedFact targetFact = f.Facts<DiceDecayedFact>().Single(x => x.DiceId == playerFive.InstanceId);
            Assert.That(sourceFact.WasWillDecay, Is.True);
            Assert.That(sourceFact.WasTargeted, Is.False);
            Assert.That(targetFact.WasWillDecay, Is.False);
            Assert.That(targetFact.WasTargeted, Is.True);
        }

        [Test]
        public void ExecuteDecay_ValidatesWholeBoardBeforeFirstCommitSoLateCorruptionCannotPartiallyResolve()
        {
            var f = new DecayFixture();
            DiceRuntimeState enemySix = f.Place(Side.Enemy, 1, 6);
            DiceRuntimeState playerFive = f.Place(Side.Player, 1, 5);
            f.Board.PlaceDice(new SlotId(Side.Enemy, 6), new DiceInstanceId(9999));

            Assert.Throws<System.InvalidOperationException>(() => f.Execute());

            Assert.That(enemySix.IsDecayedForCurrentGame, Is.False);
            Assert.That(playerFive.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Enemy, 1)).OccupantDiceId, Is.EqualTo(enemySix.InstanceId));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 1)).OccupantDiceId, Is.EqualTo(playerFive.InstanceId));
            Assert.That(f.History.Count, Is.Zero);
        }

        [Test]
        public void DecayCompletion_RejectsIfSurvivingDiceFaceChangesAfterResolution()
        {
            var f = new DecayFixture();
            DiceRuntimeState player = f.Place(Side.Player, 2, 5);
            DecayExecutionResult result = f.Execute();
            Assert.That(f.Executor.EvaluateCompletion(result), Is.EqualTo(BattleFlowDenialReason.None));

            player.SetCurrentFace(4);

            Assert.That(f.Executor.EvaluateCompletion(result), Is.EqualTo(BattleFlowDenialReason.DecayResolutionIncomplete));
        }

        [Test]
        public void CompleteDecay_RejectsWhenNoBattleControllerDecayReceiptExists()
        {
            ControllerFixture f = new ControllerFixture();
            f.EnterDecayByInternalPhasePath();

            BattleFlowResult result = f.Controller.CompleteDecay();

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.DecayNotResolved));
            Assert.That(f.State.CurrentPhase, Is.EqualTo(BattlePhase.DecayProcess));
        }

        [Test]
        public void BattleController_RequestDecayExecutesThenCompleteDecayAdvancesToScoreProcess()
        {
            ControllerFixture f = new ControllerFixture();
            f.EnterPlayerRepositionThroughController();

            BattleFlowResult request = f.Controller.RequestDecay();
            BattleFlowResult complete = f.Controller.CompleteDecay();

            Assert.That(request.IsApproved, Is.True);
            Assert.That(f.State.CurrentPhase, Is.EqualTo(BattlePhase.ScoreProcess));
            Assert.That(complete.IsApproved, Is.True);
            Assert.That(complete.Facts.Single(), Is.TypeOf<PhaseChangedFact>());
        }

        [Test]
        public void CompleteDecay_RejectsIfAuthoritativeSlotStateChangesAfterResolution()
        {
            ControllerFixture f = new ControllerFixture();
            f.EnterPlayerRepositionThroughController();
            Assert.That(f.Controller.RequestDecay().IsApproved, Is.True);
            f.Board.SetSlotCondition(new SlotId(Side.Player, 1), SlotCondition.Unstable);

            BattleFlowResult complete = f.Controller.CompleteDecay();

            Assert.That(complete.IsRejected, Is.True);
            Assert.That(complete.DenialReason, Is.EqualTo(BattleFlowDenialReason.DecayResolutionIncomplete));
            Assert.That(f.State.CurrentPhase, Is.EqualTo(BattlePhase.DecayProcess));
        }

        private static void AssertDecayed(DecayFixture f, DiceRuntimeState dice, SlotId slotId)
        {
            Assert.That(dice.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Board.GetSlot(slotId).HasDice, Is.False);
            Assert.That(f.Board.GetSlot(slotId).Condition, Is.EqualTo(SlotCondition.Broken));
        }

        private sealed class DecayFixture
        {
            private readonly Dictionary<(Side, int), DiceRuntimeState> _dice = new Dictionary<(Side, int), DiceRuntimeState>();

            internal DecayFixture()
            {
                State = DiceTestFactory.CreateBattleState();
                Board = new BoardState();
                var dice = new List<DiceRuntimeState>();
                for (int i = 1; i <= 6; i++)
                {
                    DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(i, $"dice.enemy_{i}");
                    DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(100 + i, 1000 + i, $"dice.player_{i}");
                    _dice.Add((Side.Enemy, i), enemy);
                    _dice.Add((Side.Player, i), player);
                    dice.Add(enemy);
                    dice.Add(player);
                }
                Inventory = new BattleInventoryState(10, dice);
                History = new BattleHistory();
                Executor = new DecayExecutor(State, Board, Inventory, History);
                EnterDecayPhase(State);
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

            internal void SetBroken(Side side, int slotNumber)
            {
                Board.SetSlotCondition(new SlotId(side, slotNumber), SlotCondition.Broken);
            }

            internal void SetUnstable(Side side, int slotNumber)
            {
                Board.SetSlotCondition(new SlotId(side, slotNumber), SlotCondition.Unstable);
            }

            internal DecayExecutionResult Execute() => Executor.ExecuteDecay();
            internal IEnumerable<T> Facts<T>() where T : BattleFact => History.Facts.OfType<T>();
        }

        private sealed class ControllerFixture
        {
            internal ControllerFixture()
            {
                State = DiceTestFactory.CreateBattleState();
                Board = new BoardState();
                DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(1, 1);
                DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(2);
                Inventory = new BattleInventoryState(10, new[] { player, enemy });
                History = new BattleHistory();
                PhaseController = new BattlePhaseController(State, Board, new BattlePhaseTransitionValidator(), History);
                var roll = new RollExecutor(State, Board, Inventory, History, new SeededRandomSource(1), new SeededRandomSource(2));
                var decay = new DecayExecutor(State, Board, Inventory, History);
                Controller = new BattleController(State, PhaseController, History, roll, decay);
            }

            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal BattleHistory History { get; }
            internal BattlePhaseController PhaseController { get; }
            internal BattleController Controller { get; }

            internal void EnterPlayerRepositionThroughController()
            {
                Assert.That(Controller.CompleteEnemySetup().IsApproved, Is.True);
                Assert.That(Controller.RequestRoll().IsApproved, Is.True);
                Assert.That(Controller.CompleteRoll().IsApproved, Is.True);
                Assert.That(Controller.CompleteEnemyReposition().IsApproved, Is.True);
                Assert.That(State.CurrentPhase, Is.EqualTo(BattlePhase.PlayerReposition));
            }

            internal void EnterDecayByInternalPhasePath()
            {
                EnterDecayPhase(State);
            }
        }

        private static void EnterDecayPhase(BattleState state)
        {
            if (state.CurrentPhase == BattlePhase.EnemySetup) new AdvancePhaseCommand(state, BattlePhase.PlayerSetup).Execute();
            if (state.CurrentPhase == BattlePhase.PlayerSetup) new AdvancePhaseCommand(state, BattlePhase.Rolling).Execute();
            if (state.CurrentPhase == BattlePhase.Rolling) new AdvancePhaseCommand(state, BattlePhase.EnemyReposition).Execute();
            if (state.CurrentPhase == BattlePhase.EnemyReposition) new AdvancePhaseCommand(state, BattlePhase.PlayerReposition).Execute();
            if (state.CurrentPhase == BattlePhase.PlayerReposition) new AdvancePhaseCommand(state, BattlePhase.DecayProcess).Execute();
        }
    }
}
