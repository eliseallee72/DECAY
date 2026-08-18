using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class BattleControllerTests
    {
        [Test]
        public void BattleFlow_StartsInSetup()
        {
            var fixture = new BattleFlowFixture();

            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
        }

        [Test]
        public void RequestRoll_TransitionsFromSetupAndInvokesLogicalRollExactlyOnce()
        {
            var fixture = new BattleFlowFixture(scriptedRolls: new[] { 6 });
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, new SlotId(Side.Player, 1));

            BattleFlowResult first = fixture.Controller.RequestRoll();
            BattleFlowResult second = fixture.Controller.RequestRoll();

            Assert.That(first.IsApproved, Is.True);
            Assert.That(first.Facts.Count, Is.EqualTo(2));
            Assert.That(first.Facts[0], Is.TypeOf<PhaseChangedFact>());
            Assert.That(first.Facts[1], Is.TypeOf<DiceRolledFact>());
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
            Assert.That(fixture.PlayerA.CurrentFaceIndex, Is.EqualTo(6));
            Assert.That(fixture.RandomSource.RemainingCount, Is.Zero);

            Assert.That(second.IsRejected, Is.True);
            Assert.That(second.DenialReason, Is.EqualTo(BattleFlowDenialReason.WrongPhase));
            Assert.That(fixture.History.Count, Is.EqualTo(2));
        }

        [Test]
        public void CompleteRoll_RequiresRollToHaveResolvedThroughBattleController()
        {
            var fixture = new BattleFlowFixture();
            new AdvancePhaseCommand(fixture.State, BattlePhase.Rolling).Execute();

            BattleFlowResult result = fixture.Controller.CompleteRoll();

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.RollNotResolved));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
            Assert.That(fixture.History.Count, Is.Zero);
        }

        [Test]
        public void CompleteRoll_AdvancesToEnemyRepositionOnlyAfterResolvedRollBoundary()
        {
            var fixture = new BattleFlowFixture();
            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);

            BattleFlowResult result = fixture.Controller.CompleteRoll();

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Facts.Count, Is.EqualTo(1));
            Assert.That(result.Facts[0], Is.TypeOf<PhaseChangedFact>());
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.EnemyReposition));
        }

        [Test]
        public void RequestRoll_RecoverableScriptFailureUsesFallbackAndDoesNotSoftLockRolling()
        {
            var fixture = new BattleFlowFixture(
                scriptedRolls: new[] { 4 },
                fallbackRolls: new[] { 2, 5 });
            fixture.PlaceDirect(fixture.EnemyA.InstanceId, new SlotId(Side.Enemy, 1));
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, new SlotId(Side.Player, 1));

            BattleFlowResult roll = fixture.Controller.RequestRoll();
            BattleFlowResult complete = fixture.Controller.CompleteRoll();

            Assert.That(roll.IsApproved, Is.True);
            Assert.That(fixture.EnemyA.CurrentFaceIndex, Is.EqualTo(2));
            Assert.That(fixture.PlayerA.CurrentFaceIndex, Is.EqualTo(5));
            Assert.That(complete.IsApproved, Is.True);
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.EnemyReposition));
        }

        [Test]
        public void CompleteRoll_RejectsIfACommittedCurrentRollResolutionIsNoLongerPresent()
        {
            var fixture = new BattleFlowFixture(scriptedRolls: new[] { 6 });
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, new SlotId(Side.Player, 1));
            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);
            fixture.PlayerA.ClearCurrentFace();

            BattleFlowResult result = fixture.Controller.CompleteRoll();

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.RollResolutionIncomplete));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
        }

        [Test]
        public void CompleteRoll_RejectsIfBoardGainsAnUnresolvedParticipantAfterExecution()
        {
            var fixture = new BattleFlowFixture(scriptedRolls: new[] { 6 });
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, new SlotId(Side.Player, 1));
            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);

            // Deliberately bypass normal movement Gates to simulate a corrupted/unexpected same-phase mutation.
            fixture.PlaceDirect(fixture.EnemyA.InstanceId, new SlotId(Side.Enemy, 2));
            BattleFlowResult result = fixture.Controller.CompleteRoll();

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.RollResolutionIncomplete));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
        }

        [Test]
        public void SharedMovementAuthority_AllowsBothSidesDuringSetupButKeepsRepositionSequential()
        {
            var fixture = new BattleFlowFixture(scriptedRolls: new[] { 2, 3 });
            var movement = new MoveDiceController(
                fixture.State,
                fixture.Board,
                fixture.Inventory,
                fixture.History);

            MoveDiceResult enemySetupMove = movement.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.EnemyA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Enemy, 1))));
            Assert.That(enemySetupMove.IsApproved, Is.True);

            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));

            MoveDiceResult playerSetupMove = movement.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 1))));
            Assert.That(playerSetupMove.IsApproved, Is.True);

            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteRoll().IsApproved, Is.True);
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.EnemyReposition));

            MoveDiceResult enemyRepositionMove = movement.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.EnemyA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Enemy, 2))));
            Assert.That(enemyRepositionMove.IsApproved, Is.True);

            Assert.That(fixture.Controller.CompleteEnemyReposition().IsApproved, Is.True);
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.PlayerReposition));

            MoveDiceResult playerRepositionMove = movement.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));
            Assert.That(playerRepositionMove.IsApproved, Is.True);

            BattleFlowResult decayRequest = fixture.Controller.RequestDecay();
            Assert.That(decayRequest.IsApproved, Is.True);
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.DecayProcess));
        }

        [Test]
        public void StartingNextRound_ReturnsToSetup()
        {
            var fixture = new BattleFlowFixture();
            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteRoll().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteEnemyReposition().IsApproved, Is.True);
            Assert.That(fixture.Controller.RequestDecay().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteDecay().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteScore().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteRoundEnd().IsApproved, Is.True);

            Assert.That(fixture.State.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
        }

        private sealed class BattleFlowFixture
        {
            internal BattleFlowFixture(int[] scriptedRolls = null, int[] fallbackRolls = null)
            {
                State = DiceTestFactory.CreateBattleState();
                Board = new BoardState();
                PlayerA = DiceTestFactory.CreatePlayerRuntimeDice(1, 101);
                EnemyA = DiceTestFactory.CreateEnemyRuntimeDice(2);
                Inventory = new BattleInventoryState(10, new[] { PlayerA, EnemyA });
                History = new BattleHistory();
                PhaseController = new BattlePhaseController(
                    State,
                    Board,
                    new BattlePhaseTransitionValidator(),
                    History);
                RandomSource = new ScriptedRandomSource(scriptedRolls ?? new int[0]);
                FallbackRandomSource = fallbackRolls == null
                    ? new ScriptedRandomSource(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 })
                    : new ScriptedRandomSource(fallbackRolls);
                var rollExecutor = new RollExecutor(
                    State,
                    Board,
                    Inventory,
                    History,
                    RandomSource,
                    FallbackRandomSource);
                var decayExecutor = new DecayExecutor(State, Board, Inventory, History);
                var scoreState = new ScoreState();
                var scoreExecutor = new ScoreExecutor(State, Board, Inventory, scoreState, History);
                var roundEndExecutor = new RoundEndExecutor(State, Board, Inventory, scoreState, History);
                DiceRuntimeSeed playerResetSeed = CreateNeutralSeed("dice.neutral_d6");
                DiceRuntimeSeed enemyResetSeed = CreateNeutralSeed("dice.enemy_neutral_d6");
                var global = new GlobalInventoryState(new[]
                {
                    new GlobalDiceState(new OwnedDiceId(101), playerResetSeed)
                });
                var enemyResetCatalog = new EnemyDiceResetSeedCatalog(new[]
                {
                    new System.Collections.Generic.KeyValuePair<DiceInstanceId, DiceRuntimeSeed>(EnemyA.InstanceId, enemyResetSeed)
                });
                var gameEndExecutor = new GameEndExecutor(State, Board, Inventory, global, enemyResetCatalog, scoreState, History);
                Controller = new BattleController(
                    State, PhaseController, History, scoreState, rollExecutor, decayExecutor, scoreExecutor, roundEndExecutor, gameEndExecutor);
            }

            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal BattleHistory History { get; }
            internal BattlePhaseController PhaseController { get; }
            internal ScriptedRandomSource RandomSource { get; }
            internal ScriptedRandomSource FallbackRandomSource { get; }
            internal BattleController Controller { get; }
            internal DiceRuntimeState PlayerA { get; }
            internal DiceRuntimeState EnemyA { get; }

            internal void PlaceDirect(DiceInstanceId diceId, SlotId slotId)
            {
                new PlaceDiceOnBoardCommand(State, Board, Inventory, diceId, slotId).Execute();
            }

            private static DiceRuntimeSeed CreateNeutralSeed(string definitionId)
            {
                var faces = new DiceFaceSeed[6];
                for (int i = 0; i < faces.Length; i++)
                    faces[i] = new DiceFaceSeed(i + 1, i + 1, i + 1);
                return new DiceRuntimeSeed(new DiceId(definitionId), 0, faces);
            }
        }
    }
}
