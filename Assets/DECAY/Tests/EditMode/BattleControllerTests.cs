using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class BattleControllerTests
    {
        [Test]
        public void BattleFlow_StartsWithEnemySetupAuthority()
        {
            var fixture = new BattleFlowFixture();

            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(fixture.State.CurrentSetupTurn, Is.EqualTo(BattleSetupTurn.Enemy));
        }

        [Test]
        public void CompleteEnemySetup_HandsAuthorityToPlayerAndRecordsFact()
        {
            var fixture = new BattleFlowFixture();

            BattleFlowResult result = fixture.Controller.CompleteEnemySetup();

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.None));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(fixture.State.CurrentSetupTurn, Is.EqualTo(BattleSetupTurn.Player));
            Assert.That(result.Facts.Count, Is.EqualTo(1));
            Assert.That(result.Facts[0], Is.TypeOf<SetupTurnChangedFact>());

            var fact = (SetupTurnChangedFact)result.Facts[0];
            Assert.That(fact.PreviousTurn, Is.EqualTo(BattleSetupTurn.Enemy));
            Assert.That(fact.CurrentTurn, Is.EqualTo(BattleSetupTurn.Player));
            Assert.That(fact.Context.Phase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(fact.SequenceNumber, Is.EqualTo(1));
            Assert.That(fixture.History.Facts[0], Is.SameAs(fact));
        }

        [Test]
        public void CompleteEnemySetup_CannotRunTwiceInOneSetup()
        {
            var fixture = new BattleFlowFixture();
            Assert.That(fixture.Controller.CompleteEnemySetup().IsApproved, Is.True);
            int historyCount = fixture.History.Count;

            BattleFlowResult result = fixture.Controller.CompleteEnemySetup();

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.EnemySetupAlreadyComplete));
            Assert.That(fixture.State.CurrentSetupTurn, Is.EqualTo(BattleSetupTurn.Player));
            Assert.That(fixture.History.Count, Is.EqualTo(historyCount));
        }

        [Test]
        public void RequestRoll_IsRejectedUntilEnemySetupCompletes()
        {
            var fixture = new BattleFlowFixture(scriptedRolls: new[] { 6 });
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, new SlotId(Side.Player, 1));

            BattleFlowResult result = fixture.Controller.RequestRoll();

            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(BattleFlowDenialReason.EnemySetupMustComplete));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(fixture.RandomSource.RemainingCount, Is.EqualTo(1));
            Assert.That(fixture.History.Count, Is.Zero);
        }

        [Test]
        public void RequestRoll_TransitionsToRollingAndInvokesLogicalRollExactlyOnce()
        {
            var fixture = new BattleFlowFixture(scriptedRolls: new[] { 6 });
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, new SlotId(Side.Player, 1));
            Assert.That(fixture.Controller.CompleteEnemySetup().IsApproved, Is.True);

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
            Assert.That(fixture.History.Count, Is.EqualTo(3));
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
            Assert.That(fixture.Controller.CompleteEnemySetup().IsApproved, Is.True);
            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);

            BattleFlowResult result = fixture.Controller.CompleteRoll();

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Facts.Count, Is.EqualTo(1));
            Assert.That(result.Facts[0], Is.TypeOf<PhaseChangedFact>());
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.EnemyReposition));
        }

        [Test]
        public void SharedMovementAuthority_FollowsEnemySetupPlayerSetupEnemyRepositionPlayerReposition()
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

            Assert.That(fixture.Controller.CompleteEnemySetup().IsApproved, Is.True);

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
        public void StartingNextRound_ResetsSetupAuthorityToEnemy()
        {
            var fixture = new BattleFlowFixture();
            Assert.That(fixture.Controller.CompleteEnemySetup().IsApproved, Is.True);
            Assert.That(fixture.Controller.RequestRoll().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteRoll().IsApproved, Is.True);
            Assert.That(fixture.Controller.CompleteEnemyReposition().IsApproved, Is.True);
            Assert.That(fixture.Controller.RequestDecay().IsApproved, Is.True);

            Assert.That(fixture.PhaseController.Handle(new PhaseChangeRequest(BattlePhase.ScoreProcess)).IsApproved, Is.True);
            Assert.That(fixture.PhaseController.Handle(new PhaseChangeRequest(BattlePhase.RoundEnd)).IsApproved, Is.True);
            Assert.That(fixture.PhaseController.Handle(new PhaseChangeRequest(BattlePhase.Setup)).IsApproved, Is.True);

            Assert.That(fixture.State.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(fixture.State.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(fixture.State.CurrentSetupTurn, Is.EqualTo(BattleSetupTurn.Enemy));
        }

        private sealed class BattleFlowFixture
        {
            internal BattleFlowFixture(int[] scriptedRolls = null)
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
                var rollExecutor = new RollExecutor(State, Board, Inventory, History, RandomSource);
                Controller = new BattleController(State, PhaseController, History, rollExecutor);
            }

            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal BattleHistory History { get; }
            internal BattlePhaseController PhaseController { get; }
            internal ScriptedRandomSource RandomSource { get; }
            internal BattleController Controller { get; }
            internal DiceRuntimeState PlayerA { get; }
            internal DiceRuntimeState EnemyA { get; }

            internal void PlaceDirect(DiceInstanceId diceId, SlotId slotId)
            {
                new PlaceDiceOnBoardCommand(State, Board, Inventory, diceId, slotId).Execute();
            }
        }
    }
}
