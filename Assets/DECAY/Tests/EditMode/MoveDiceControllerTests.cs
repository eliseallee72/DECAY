using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class MoveDiceControllerTests
    {
        [Test]
        public void Setup_InventoryToEmptyOwnSlot_IsApprovedAndRecorded()
        {
            var fixture = new MovementFixture();
            SlotId destination = new SlotId(Side.Player, 3);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(MoveDiceDenialReason.None));
            Assert.That(result.Fact, Is.TypeOf<DicePlacedOnBoardFact>());
            Assert.That(result.Fact.SequenceNumber, Is.EqualTo(1));
            Assert.That(fixture.History.Count, Is.EqualTo(1));
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.False);
        }

        [Test]
        public void Setup_InventoryToOccupiedOwnSlot_UsesBoardInventorySwap()
        {
            var fixture = new MovementFixture();
            SlotId destination = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, destination);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerB.InstanceId,
                MoveDiceTarget.Board(destination)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<BoardInventoryDiceSwappedFact>());
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.PlayerB.InstanceId));
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerB.InstanceId), Is.False);
        }

        [Test]
        public void Setup_BoardToInventory_IsApprovedAndPreservesSameRuntimeDice()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 1);
            fixture.PlayerA.SetGeneralScoreValue(17);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.BattleInventory));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<DiceReturnedToInventoryFact>());
            Assert.That(fixture.Board.GetSlot(source).HasDice, Is.False);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
            Assert.That(fixture.Inventory.GetDice(fixture.PlayerA.InstanceId), Is.SameAs(fixture.PlayerA));
            Assert.That(fixture.PlayerA.GeneralScoreValue, Is.EqualTo(17));
        }

        [Test]
        public void Setup_BoardToEmptyOwnSlot_IsApprovedAsBoardMove()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 1);
            SlotId destination = new SlotId(Side.Player, 5);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<DiceMovedOnBoardFact>());
            Assert.That(fixture.Board.GetSlot(source).HasDice, Is.False);
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
        }

        [Test]
        public void Setup_BoardToOccupiedOwnSlot_IsApprovedAsBoardSwap()
        {
            var fixture = new MovementFixture();
            SlotId first = new SlotId(Side.Player, 1);
            SlotId second = new SlotId(Side.Player, 4);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, first);
            fixture.PlaceDirect(fixture.PlayerB.InstanceId, second);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(second)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<BoardDiceSwappedFact>());
            Assert.That(fixture.Board.GetSlot(first).OccupantDiceId, Is.EqualTo(fixture.PlayerB.InstanceId));
            Assert.That(fixture.Board.GetSlot(second).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
        }

        [Test]
        public void Setup_CrossSideBoardTarget_IsRejectedWithoutMutationOrFact()
        {
            var fixture = new MovementFixture();
            SlotId enemyDestination = new SlotId(Side.Enemy, 3);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(enemyDestination)));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DestinationSideMismatch);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
            Assert.That(fixture.Board.GetSlot(enemyDestination).HasDice, Is.False);
        }

        [Test]
        public void Setup_BrokenDestination_IsRejectedWithoutMutation()
        {
            var fixture = new MovementFixture();
            SlotId destination = new SlotId(Side.Player, 3);
            fixture.SetSlotCondition(destination, SlotCondition.Broken);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DestinationSlotUnavailable);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
        }

        [Test]
        public void Setup_UnstableDestination_IsRejectedWithoutMutation()
        {
            var fixture = new MovementFixture();
            SlotId destination = new SlotId(Side.Player, 4);
            fixture.SetSlotCondition(destination, SlotCondition.Unstable);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DestinationSlotUnavailable);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
        }

        [Test]
        public void Setup_ActingSideCannotControlOtherSidesDice()
        {
            var fixture = new MovementFixture();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Enemy, 1))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DiceOwnedByOtherSide);
        }

        [Test]
        public void Setup_AllowsPlayerAndEnemyMovementWithoutSubturnOwnership()
        {
            var fixture = new MovementFixture();

            MoveDiceResult enemyMove = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.EnemyA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Enemy, 1))));
            MoveDiceResult playerMove = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 1))));

            Assert.That(enemyMove.IsApproved, Is.True);
            Assert.That(playerMove.IsApproved, Is.True);
            Assert.That(fixture.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(fixture.History.Count, Is.EqualTo(2));
        }

        [Test]
        public void PlayerReposition_BoardToEmptyOwnSlot_IsApproved()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 2);
            SlotId destination = new SlotId(Side.Player, 6);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);
            fixture.AdvanceToPlayerReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<DiceMovedOnBoardFact>());
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
        }

        [Test]
        public void PlayerReposition_BoardToOccupiedOwnSlot_IsApprovedAsSwap()
        {
            var fixture = new MovementFixture();
            SlotId first = new SlotId(Side.Player, 2);
            SlotId second = new SlotId(Side.Player, 6);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, first);
            fixture.PlaceDirect(fixture.PlayerB.InstanceId, second);
            fixture.AdvanceToPlayerReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(second)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<BoardDiceSwappedFact>());
        }

        [Test]
        public void PlayerReposition_BoardToInventory_IsRejected()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);
            fixture.AdvanceToPlayerReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.BattleInventory));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.InventoryNotAllowedDuringReposition);
            Assert.That(fixture.Board.GetSlot(source).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
        }

        [Test]
        public void PlayerReposition_InventoryToBoard_IsRejected()
        {
            var fixture = new MovementFixture();
            fixture.AdvanceToPlayerReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 3))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.RepositionRequiresBoardSource);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
        }

        [Test]
        public void EnemyReposition_EnemyBoardMovement_IsApprovedThroughSameController()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Enemy, 1);
            SlotId destination = new SlotId(Side.Enemy, 4);
            fixture.PlaceDirect(fixture.EnemyA.InstanceId, source);
            fixture.AdvanceToEnemyReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.EnemyA.InstanceId,
                MoveDiceTarget.Board(destination)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<DiceMovedOnBoardFact>());
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.EnemyA.InstanceId));
        }

        [Test]
        public void EnemyReposition_PlayerMovement_IsRejectedByPhaseSideGate()
        {
            var fixture = new MovementFixture();
            SlotId playerSlot = new SlotId(Side.Player, 1);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, playerSlot);
            fixture.AdvanceToEnemyReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.ActingSideDoesNotMatchPhase);
        }

        [Test]
        public void PlayerReposition_EnemyMovement_IsRejectedByPhaseSideGate()
        {
            var fixture = new MovementFixture();
            SlotId enemySlot = new SlotId(Side.Enemy, 1);
            fixture.PlaceDirect(fixture.EnemyA.InstanceId, enemySlot);
            fixture.AdvanceToPlayerReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.EnemyA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Enemy, 2))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.ActingSideDoesNotMatchPhase);
        }

        [TestCase(BattlePhase.Rolling)]
        [TestCase(BattlePhase.DecayProcess)]
        [TestCase(BattlePhase.ScoreProcess)]
        [TestCase(BattlePhase.RoundEnd)]
        [TestCase(BattlePhase.GameEnd)]
        public void NonMovementPhase_DoesNotPermitMovement(BattlePhase phase)
        {
            var fixture = new MovementFixture();
            fixture.Advance(phase);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.PhaseDoesNotAllowMovement);
        }

        [Test]
        public void DecayedTrackedDiceOutsideInventory_IsUnavailableForMovement()
        {
            var fixture = new MovementFixture();
            fixture.Inventory.RemoveFromInventory(fixture.PlayerA.InstanceId);
            fixture.PlayerA.MarkDecayedForCurrentGame();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DiceUnavailable);
            Assert.That(fixture.Board.IsDiceOnBoard(fixture.PlayerA.InstanceId), Is.False);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.False);
        }

        [Test]
        public void BoardDiceRequestedToItsCurrentSlot_IsRejectedAsNoOp()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(source)));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.AlreadyAtDestination);
        }

        [Test]
        public void InventoryDiceRequestedToInventory_IsRejectedAsNoOp()
        {
            var fixture = new MovementFixture();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.BattleInventory));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.AlreadyAtDestination);
        }

        [Test]
        public void DiceInUnstableSourceSlot_IsUnavailableForInteractiveMovement()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);
            fixture.SetSlotCondition(source, SlotCondition.Unstable);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 3))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.SourceSlotUnavailable);
            Assert.That(fixture.Board.GetSlot(source).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
        }

        [Test]
        public void DicePresentOnBoardAndInventory_IsRejectedAsInvalidSourceState()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);
            fixture.Inventory.ReturnToInventory(fixture.PlayerA.InstanceId);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 3))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.SourceStateInvalid);
            Assert.That(fixture.Board.GetSlot(source).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
        }

        [Test]
        public void OccupiedDestinationWithInvalidMembershipState_IsRejectedBeforeCommand()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 1);
            SlotId destination = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);
            fixture.PlaceDirect(fixture.PlayerB.InstanceId, destination);
            fixture.Inventory.ReturnToInventory(fixture.PlayerB.InstanceId);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DestinationDiceStateInvalid);
            Assert.That(fixture.Board.GetSlot(source).OccupantDiceId, Is.EqualTo(fixture.PlayerA.InstanceId));
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.PlayerB.InstanceId));
        }

        [Test]
        public void ApprovedMovementFact_UsesAuthoritativePhaseAtExecutionAndIsRecordedOnce()
        {
            var fixture = new MovementFixture();
            SlotId source = new SlotId(Side.Player, 2);
            SlotId destination = new SlotId(Side.Player, 3);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);
            fixture.AdvanceToPlayerReposition();

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            var fact = (DiceMovedOnBoardFact)result.Fact;
            Assert.That(fact.Context.Phase, Is.EqualTo(BattlePhase.PlayerReposition));
            Assert.That(fact.Context.GameNumber, Is.EqualTo(1));
            Assert.That(fact.Context.RoundNumber, Is.EqualTo(1));
            Assert.That(fact.SequenceNumber, Is.EqualTo(1));
            Assert.That(fixture.History.Facts[0], Is.SameAs(fact));
        }

        [Test]
        public void AdditionalGate_CanBlockOtherwiseLegalMoveWithoutDuplicatingCoreRules()
        {
            var fixture = new MovementFixture(additionalGates: new IMoveDiceGate[] { new AlwaysBlockTutorialGate() });
            SlotId destination = new SlotId(Side.Player, 3);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(destination)));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.TutorialRestriction);
            Assert.That(fixture.Inventory.IsInInventory(fixture.PlayerA.InstanceId), Is.True);
            Assert.That(fixture.Board.GetSlot(destination).HasDice, Is.False);
        }

        [Test]
        public void Setup_EnemyInventoryToOwnEmptySlot_IsApprovedThroughSameController()
        {
            var fixture = new MovementFixture();
            SlotId destination = new SlotId(Side.Enemy, 3);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Enemy,
                fixture.EnemyA.InstanceId,
                MoveDiceTarget.Board(destination)));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.Fact, Is.TypeOf<DicePlacedOnBoardFact>());
            Assert.That(fixture.Board.GetSlot(destination).OccupantDiceId, Is.EqualTo(fixture.EnemyA.InstanceId));
            Assert.That(fixture.Inventory.IsInInventory(fixture.EnemyA.InstanceId), Is.False);
            Assert.That(fixture.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void UntrackedDiceRequest_IsRejectedWithoutChangingStateOrHistory()
        {
            var fixture = new MovementFixture();
            var unknownDiceId = new DiceInstanceId(999);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                unknownDiceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.DiceNotTracked);
            Assert.That(fixture.Board.IsDiceOnBoard(unknownDiceId), Is.False);
        }

        [Test]
        public void AdditionalGate_ReceivesAuthoritativelyResolvedSourceRatherThanCallerSuppliedLocation()
        {
            var sourceGate = new CaptureSourceGate();
            var fixture = new MovementFixture(additionalGates: new IMoveDiceGate[] { sourceGate });
            SlotId source = new SlotId(Side.Player, 2);
            fixture.PlaceDirect(fixture.PlayerA.InstanceId, source);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 3))));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(sourceGate.WasEvaluated, Is.True);
            Assert.That(sourceGate.ResolvedSource, Is.EqualTo(source));
            Assert.That(sourceGate.ResolvedAsBoardDice, Is.True);
            Assert.That(sourceGate.ResolvedAsInventoryDice, Is.False);
        }

        [Test]
        public void BattleEnd_RejectsMovementAsBattleAlreadyComplete()
        {
            var fixture = new MovementFixture();
            fixture.Advance(BattlePhase.BattleEnd);

            MoveDiceResult result = fixture.Controller.RequestMove(new MoveDiceRequest(
                Side.Player,
                fixture.PlayerA.InstanceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));

            AssertRejectedWithoutHistory(result, fixture, MoveDiceDenialReason.BattleAlreadyComplete);
        }

        private static void AssertRejectedWithoutHistory(
            MoveDiceResult result,
            MovementFixture fixture,
            MoveDiceDenialReason expectedReason)
        {
            Assert.That(result.IsRejected, Is.True);
            Assert.That(result.IsApproved, Is.False);
            Assert.That(result.DenialReason, Is.EqualTo(expectedReason));
            Assert.That(result.Fact, Is.Null);
            Assert.That(fixture.History.Count, Is.EqualTo(0));
        }

        private sealed class AlwaysBlockTutorialGate : IMoveDiceGate
        {
            public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
            {
                return MoveDiceDenialReason.TutorialRestriction;
            }
        }

        private sealed class CaptureSourceGate : IMoveDiceGate
        {
            internal bool WasEvaluated { get; private set; }
            internal SlotId ResolvedSource { get; private set; }
            internal bool ResolvedAsBoardDice { get; private set; }
            internal bool ResolvedAsInventoryDice { get; private set; }

            public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
            {
                WasEvaluated = true;
                ResolvedSource = context.SourceSlot;
                ResolvedAsBoardDice = context.IsOnBoard;
                ResolvedAsInventoryDice = context.IsInInventory;
                return MoveDiceDenialReason.None;
            }
        }

        private sealed class MovementFixture
        {
            internal MovementFixture(
                System.Collections.Generic.IEnumerable<IMoveDiceGate> additionalGates = null)
            {
                BattleState = DiceTestFactory.CreateBattleState();
                Board = new BoardState();
                PlayerA = DiceTestFactory.CreatePlayerRuntimeDice(1, 101);
                PlayerB = DiceTestFactory.CreatePlayerRuntimeDice(2, 102, "dice.player_second_d6");
                EnemyA = DiceTestFactory.CreateEnemyRuntimeDice(3);
                Inventory = new BattleInventoryState(10, new[] { PlayerA, PlayerB, EnemyA });
                History = new BattleHistory();
                Controller = new MoveDiceController(BattleState, Board, Inventory, History, additionalGates);
            }

            internal BattleState BattleState { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal BattleHistory History { get; }
            internal MoveDiceController Controller { get; }
            internal DiceRuntimeState PlayerA { get; }
            internal DiceRuntimeState PlayerB { get; }
            internal DiceRuntimeState EnemyA { get; }

            internal void PlaceDirect(DiceInstanceId diceId, SlotId slotId)
            {
                new PlaceDiceOnBoardCommand(BattleState, Board, Inventory, diceId, slotId).Execute();
            }

            internal void SetSlotCondition(SlotId slotId, SlotCondition condition)
            {
                new SetSlotConditionCommand(BattleState, Board, slotId, condition).Execute();
            }

            internal void Advance(BattlePhase phase)
            {
                new AdvancePhaseCommand(BattleState, phase).Execute();
            }

            internal void AdvanceToEnemyReposition()
            {
                Advance(BattlePhase.Rolling);
                Advance(BattlePhase.EnemyReposition);
            }

            internal void AdvanceToPlayerReposition()
            {
                AdvanceToEnemyReposition();
                Advance(BattlePhase.PlayerReposition);
            }
        }
    }
}
