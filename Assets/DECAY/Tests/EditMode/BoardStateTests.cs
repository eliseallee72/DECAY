using System;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class BoardStateTests
    {
        [Test]
        public void BoardState_CreatesTwelveAuthoritativeSlotsAsUnbrokenAndEmpty()
        {
            var board = new BoardState();

            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                AssertEmptyUnbroken(board.GetSlot(new SlotId(Side.Enemy, number)));
                AssertEmptyUnbroken(board.GetSlot(new SlotId(Side.Player, number)));
            }
        }

        [Test]
        public void PlaceCommand_TransfersMembershipFromInventoryToBoardAndProducesFact()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(101, 201);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId destination = new SlotId(Side.Player, 3);

            var command = new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, destination);
            DicePlacedOnBoardFact fact = command.Execute();

            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.False);
            Assert.That(board.GetSlot(destination).OccupantDiceId, Is.EqualTo(dice.InstanceId));
            Assert.That(board.TryGetSlotOfDice(dice.InstanceId, out SlotId actualSlot), Is.True);
            Assert.That(actualSlot, Is.EqualTo(destination));
            Assert.That(fact.Context, Is.EqualTo(battle.CurrentFactContext));
            Assert.That(fact.DiceId, Is.EqualTo(dice.InstanceId));
            Assert.That(fact.Side, Is.EqualTo(Side.Player));
            Assert.That(fact.DestinationSlot, Is.EqualTo(destination));
        }

        [Test]
        public void PlaceCommand_RejectsOpposingSideAndLeavesBothOwnersUnchanged()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(102, 202);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId enemySlot = new SlotId(Side.Enemy, 2);

            var command = new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, enemySlot);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.True);
            Assert.That(board.GetSlot(enemySlot).HasDice, Is.False);
        }

        [Test]
        public void ReturnCommand_VacatesBoardAndRestoresInventoryMembershipWithoutResettingDice()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(103, 203);
            dice.SetGeneralScoreValue(9);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 4);
            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, slot).Execute();

            var command = new ReturnDiceToInventoryCommand(battle, board, inventory, slot);
            DiceReturnedToInventoryFact fact = command.Execute();

            Assert.That(board.GetSlot(slot).HasDice, Is.False);
            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.True);
            Assert.That(inventory.GetDice(dice.InstanceId).GeneralScoreValue, Is.EqualTo(9));
            Assert.That(fact.SourceSlot, Is.EqualTo(slot));
        }

        [Test]
        public void BoardMoveCommand_MovesWithinSameSideButNotIntoInventory()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(104, 204);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId source = new SlotId(Side.Player, 1);
            SlotId destination = new SlotId(Side.Player, 5);
            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, source).Execute();

            DiceMovedOnBoardFact fact = new MoveDiceOnBoardCommand(battle, board, inventory, source, destination).Execute();

            Assert.That(board.GetSlot(source).HasDice, Is.False);
            Assert.That(board.GetSlot(destination).OccupantDiceId, Is.EqualTo(dice.InstanceId));
            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.False);
            Assert.That(fact.SourceSlot, Is.EqualTo(source));
            Assert.That(fact.DestinationSlot, Is.EqualTo(destination));
        }

        [Test]
        public void BoardMoveCommand_RejectsCrossSideMovement()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(105, 205);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId source = new SlotId(Side.Player, 1);
            SlotId enemyDestination = new SlotId(Side.Enemy, 1);
            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, source).Execute();

            var command = new MoveDiceOnBoardCommand(battle, board, inventory, source, enemyDestination);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(board.GetSlot(source).OccupantDiceId, Is.EqualTo(dice.InstanceId));
            Assert.That(board.GetSlot(enemyDestination).HasDice, Is.False);
        }

        [Test]
        public void SwapCommand_ExchangesTwoOccupiedSlotsOnSameSide()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState first = DiceTestFactory.CreatePlayerRuntimeDice(106, 206, "dice.first");
            DiceRuntimeState second = DiceTestFactory.CreatePlayerRuntimeDice(107, 207, "dice.second");
            var inventory = new BattleInventoryState(10, new[] { first, second });
            var board = new BoardState();
            SlotId firstSlot = new SlotId(Side.Player, 2);
            SlotId secondSlot = new SlotId(Side.Player, 6);
            new PlaceDiceOnBoardCommand(battle, board, inventory, first.InstanceId, firstSlot).Execute();
            new PlaceDiceOnBoardCommand(battle, board, inventory, second.InstanceId, secondSlot).Execute();

            BoardDiceSwappedFact fact = new SwapBoardDiceCommand(battle, board, inventory, firstSlot, secondSlot).Execute();

            Assert.That(board.GetSlot(firstSlot).OccupantDiceId, Is.EqualTo(second.InstanceId));
            Assert.That(board.GetSlot(secondSlot).OccupantDiceId, Is.EqualTo(first.InstanceId));
            Assert.That(fact.FirstDiceId, Is.EqualTo(first.InstanceId));
            Assert.That(fact.FirstDestinationSlot, Is.EqualTo(secondSlot));
        }

        [Test]
        public void BrokenSlot_CannotReceiveDice()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(108, 208);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 3);
            new SetSlotConditionCommand(battle, board, slot, SlotCondition.Broken).Execute();

            var command = new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, slot);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.True);
            Assert.That(board.GetSlot(slot).HasDice, Is.False);
        }

        [Test]
        public void SlotCannotBecomeBrokenUntilItsDiceHasBeenRemoved()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(109, 209);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 4);
            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, slot).Execute();

            var breakCommand = new SetSlotConditionCommand(battle, board, slot, SlotCondition.Broken);
            Assert.Throws<InvalidOperationException>(() => breakCommand.Execute());

            new ReturnDiceToInventoryCommand(battle, board, inventory, slot).Execute();
            SlotConditionChangedFact fact = new SetSlotConditionCommand(battle, board, slot, SlotCondition.Broken).Execute();

            Assert.That(board.GetSlot(slot).Condition, Is.EqualTo(SlotCondition.Broken));
            Assert.That(fact.PreviousCondition, Is.EqualTo(SlotCondition.Unbroken));
            Assert.That(fact.CurrentCondition, Is.EqualTo(SlotCondition.Broken));
        }

        [Test]
        public void BoardState_ReportsWhenEverySlotOnOneSideIsBroken()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            var board = new BoardState();

            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                new SetSlotConditionCommand(battle, board, new SlotId(Side.Enemy, number), SlotCondition.Broken).Execute();
            }

            Assert.That(board.BrokenSlotCount(Side.Enemy), Is.EqualTo(6));
            Assert.That(board.AreAllSlotsBroken(Side.Enemy), Is.True);
            Assert.That(board.AreAllSlotsBroken(Side.Player), Is.False);
        }

        [Test]
        public void BoardInventorySwap_AtomicallyExchangesDiceWithoutResettingEitherDice()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState boardDice = DiceTestFactory.CreatePlayerRuntimeDice(111, 211, "dice.board");
            DiceRuntimeState inventoryDice = DiceTestFactory.CreatePlayerRuntimeDice(112, 212, "dice.inventory");
            boardDice.SetGeneralScoreValue(7);
            inventoryDice.SetGeneralScoreValue(12);
            var inventory = new BattleInventoryState(10, new[] { boardDice, inventoryDice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 2);
            new PlaceDiceOnBoardCommand(battle, board, inventory, boardDice.InstanceId, slot).Execute();

            BoardInventoryDiceSwappedFact fact = new SwapBoardWithInventoryCommand(
                battle,
                board,
                inventory,
                slot,
                inventoryDice.InstanceId).Execute();

            Assert.That(board.GetSlot(slot).OccupantDiceId, Is.EqualTo(inventoryDice.InstanceId));
            Assert.That(inventory.IsInInventory(inventoryDice.InstanceId), Is.False);
            Assert.That(inventory.IsInInventory(boardDice.InstanceId), Is.True);
            Assert.That(inventory.GetDice(boardDice.InstanceId).GeneralScoreValue, Is.EqualTo(7));
            Assert.That(inventory.GetDice(inventoryDice.InstanceId).GeneralScoreValue, Is.EqualTo(12));
            Assert.That(fact.BoardToInventoryDiceId, Is.EqualTo(boardDice.InstanceId));
            Assert.That(fact.InventoryToBoardDiceId, Is.EqualTo(inventoryDice.InstanceId));
            Assert.That(fact.SlotId, Is.EqualTo(slot));
            Assert.That(fact.Context, Is.EqualTo(battle.CurrentFactContext));
        }

        [Test]
        public void BoardInventorySwap_RejectsOpposingSideWithoutChangingMembership()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState playerBoardDice = DiceTestFactory.CreatePlayerRuntimeDice(113, 213);
            DiceRuntimeState enemyInventoryDice = DiceTestFactory.CreateEnemyRuntimeDice(114);
            var inventory = new BattleInventoryState(10, new[] { playerBoardDice, enemyInventoryDice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 1);
            new PlaceDiceOnBoardCommand(battle, board, inventory, playerBoardDice.InstanceId, slot).Execute();

            var command = new SwapBoardWithInventoryCommand(
                battle,
                board,
                inventory,
                slot,
                enemyInventoryDice.InstanceId);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(board.GetSlot(slot).OccupantDiceId, Is.EqualTo(playerBoardDice.InstanceId));
            Assert.That(inventory.IsInInventory(playerBoardDice.InstanceId), Is.False);
            Assert.That(inventory.IsInInventory(enemyInventoryDice.InstanceId), Is.True);
        }

        [Test]
        public void MoveCommand_RequiresSourceSlotToBeUnbroken()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(115, 215);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId source = new SlotId(Side.Player, 1);
            SlotId destination = new SlotId(Side.Player, 2);
            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, source).Execute();
            new SetSlotConditionCommand(battle, board, source, SlotCondition.Unstable).Execute();

            var command = new MoveDiceOnBoardCommand(battle, board, inventory, source, destination);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(board.GetSlot(source).OccupantDiceId, Is.EqualTo(dice.InstanceId));
            Assert.That(board.GetSlot(destination).HasDice, Is.False);
        }

        [Test]
        public void SlotConditionCommand_RejectsNoOpBecauseFactsRepresentActualChanges()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 1);
            var command = new SetSlotConditionCommand(battle, board, slot, SlotCondition.Unbroken);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(board.GetSlot(slot).Condition, Is.EqualTo(SlotCondition.Unbroken));
        }

        [Test]
        public void ApprovedCommand_CanOnlyExecuteOnce()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(110, 210);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            var command = new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, new SlotId(Side.Player, 1));

            command.Execute();

            Assert.Throws<InvalidOperationException>(() => command.Execute());
        }


        [Test]
        public void UnstableSlotReturn_PreservesBattleMutationsAndReturnsSameDiceToInventory()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(116, 216);
            dice.SetGeneralScoreValue(14);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 3);

            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, slot).Execute();
            new SetSlotConditionCommand(battle, board, slot, SlotCondition.Unstable).Execute();

            DiceReturnedToInventoryFact fact =
                new ReturnDiceToInventoryCommand(battle, board, inventory, slot).Execute();

            Assert.That(board.GetSlot(slot).HasDice, Is.False);
            Assert.That(board.GetSlot(slot).Condition, Is.EqualTo(SlotCondition.Unstable));
            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.True);
            Assert.That(inventory.GetDice(dice.InstanceId), Is.SameAs(dice));
            Assert.That(dice.GeneralScoreValue, Is.EqualTo(14));
            Assert.That(dice.IsDecayedForCurrentGame, Is.False);
            Assert.That(fact.DiceId, Is.EqualTo(dice.InstanceId));

            new SetSlotConditionCommand(battle, board, slot, SlotCondition.Broken).Execute();
            Assert.That(board.GetSlot(slot).Condition, Is.EqualTo(SlotCondition.Broken));
        }

        [Test]
        public void ReturnCommand_RejectsDecayedDiceWithoutSplittingBoardAndInventoryAuthority()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(117, 217);
            var inventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 4);

            new PlaceDiceOnBoardCommand(battle, board, inventory, dice.InstanceId, slot).Execute();
            dice.MarkDecayedForCurrentGame();

            var command = new ReturnDiceToInventoryCommand(battle, board, inventory, slot);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(board.GetSlot(slot).OccupantDiceId, Is.EqualTo(dice.InstanceId));
            Assert.That(inventory.IsInInventory(dice.InstanceId), Is.False);
        }

        [Test]
        public void CommandFactContext_IsTakenFromBattleStateWhenCommandExecutes()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            var board = new BoardState();
            SlotId slot = new SlotId(Side.Player, 1);
            var command = new SetSlotConditionCommand(battle, board, slot, SlotCondition.Unstable);

            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            SlotConditionChangedFact fact = command.Execute();

            Assert.That(fact.Context, Is.EqualTo(new BattleFactContext(1, 1, BattlePhase.Rolling)));
        }

        private static void AssertEmptyUnbroken(SlotState slot)
        {
            Assert.That(slot.Id.IsValid, Is.True);
            Assert.That(slot.Condition, Is.EqualTo(SlotCondition.Unbroken));
            Assert.That(slot.HasDice, Is.False);
            Assert.That(slot.OccupantDiceId.IsValid, Is.False);
        }
    }
}
