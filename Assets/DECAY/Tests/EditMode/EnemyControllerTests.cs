using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class EnemyControllerTests
    {
        [Test]
        public void ExecuteSetup_FillsAvailableEnemySlotsLeftToRightThroughSharedMovementAuthority()
        {
            var fixture = new EnemyFixture();

            EnemySetupExecutionResult result = fixture.Controller.ExecuteSetup();

            Assert.That(result.MovementCount, Is.EqualTo(2));
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 1)).OccupantDiceId, Is.EqualTo(fixture.EnemyA.InstanceId));
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 2)).OccupantDiceId, Is.EqualTo(fixture.EnemyB.InstanceId));
            Assert.That(fixture.Inventory.IsInInventory(fixture.EnemyA.InstanceId), Is.False);
            Assert.That(fixture.Inventory.IsInInventory(fixture.EnemyB.InstanceId), Is.False);
            Assert.That(fixture.History.Count, Is.EqualTo(2));
            Assert.That(fixture.History.Facts[0], Is.TypeOf<DicePlacedOnBoardFact>());
            Assert.That(fixture.History.Facts[1], Is.TypeOf<DicePlacedOnBoardFact>());
        }

        [Test]
        public void FillPlanner_SkipsOccupiedBrokenAndUnstableSlotsDeterministically()
        {
            var fixture = new EnemyFixture();
            fixture.PlaceDirect(fixture.EnemyA.InstanceId, new SlotId(Side.Enemy, 1));
            fixture.SetCondition(new SlotId(Side.Enemy, 2), SlotCondition.Broken);
            fixture.SetCondition(new SlotId(Side.Enemy, 3), SlotCondition.Unstable);

            EnemySetupExecutionResult result = fixture.Controller.ExecuteSetup();

            Assert.That(result.MovementCount, Is.EqualTo(1));
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 4)).OccupantDiceId, Is.EqualTo(fixture.EnemyB.InstanceId));
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 2)).HasDice, Is.False);
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 3)).HasDice, Is.False);
        }

        [Test]
        public void ExecuteSetup_UsesInjectedPlannerWithoutChangingMovementAuthority()
        {
            var fixture = new EnemyFixture(new SingleSlotPlanner(2, 6));

            EnemySetupExecutionResult result = fixture.Controller.ExecuteSetup();

            Assert.That(result.MovementCount, Is.EqualTo(1));
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 6)).OccupantDiceId, Is.EqualTo(fixture.EnemyB.InstanceId));
            Assert.That(fixture.Inventory.IsInInventory(fixture.EnemyA.InstanceId), Is.True);
            Assert.That(fixture.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteSetup_InvalidPlannerRequestFailsBeforeThatRequestMutatesBoard()
        {
            var fixture = new EnemyFixture(new ValidThenInvalidPlanner());

            Assert.Throws<InvalidOperationException>(() => fixture.Controller.ExecuteSetup());

            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 1)).HasDice, Is.False);
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 2)).HasDice, Is.False);
            Assert.That(fixture.Inventory.IsInInventory(fixture.EnemyA.InstanceId), Is.True);
            Assert.That(fixture.History.Count, Is.Zero);
        }

        [Test]
        public void ExecuteSetup_RequiresSharedSetupPhase()
        {
            var fixture = new EnemyFixture();
            new AdvancePhaseCommand(fixture.State, BattlePhase.Rolling).Execute();

            Assert.Throws<InvalidOperationException>(() => fixture.Controller.ExecuteSetup());
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 1)).HasDice, Is.False);
            Assert.That(fixture.Board.GetSlot(new SlotId(Side.Enemy, 2)).HasDice, Is.False);
        }

        private sealed class EnemyFixture
        {
            internal EnemyFixture(IEnemySetupPlanner planner = null)
            {
                State = DiceTestFactory.CreateBattleState();
                Board = new BoardState();
                EnemyA = DiceTestFactory.CreateEnemyRuntimeDice(1);
                EnemyB = DiceTestFactory.CreateEnemyRuntimeDice(2, "dice.enemy_second_d6");
                Inventory = new BattleInventoryState(10, new[] { EnemyA, EnemyB });
                History = new BattleHistory();
                var movement = new MoveDiceController(State, Board, Inventory, History);
                Controller = new EnemyController(
                    State,
                    Board,
                    Inventory,
                    movement,
                    planner ?? new FillAvailableEnemySlotsPlanner());
            }

            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal BattleHistory History { get; }
            internal EnemyController Controller { get; }
            internal DiceRuntimeState EnemyA { get; }
            internal DiceRuntimeState EnemyB { get; }

            internal void PlaceDirect(DiceInstanceId diceId, SlotId slotId)
            {
                new PlaceDiceOnBoardCommand(State, Board, Inventory, diceId, slotId).Execute();
            }

            internal void SetCondition(SlotId slotId, SlotCondition condition)
            {
                new SetSlotConditionCommand(State, Board, slotId, condition).Execute();
            }
        }

        private sealed class SingleSlotPlanner : IEnemySetupPlanner
        {
            private readonly int _inventoryIndex;
            private readonly int _slotNumber;

            internal SingleSlotPlanner(int inventoryIndex, int slotNumber)
            {
                _inventoryIndex = inventoryIndex;
                _slotNumber = slotNumber;
            }

            public IReadOnlyList<MoveDiceRequest> CreatePlan(
                BattleState battleState,
                BoardState boardState,
                BattleInventoryState battleInventoryState)
            {
                IReadOnlyList<DiceInstanceId> enemyInventory = battleInventoryState.InventoryDiceIds(Side.Enemy);
                return new[]
                {
                    new MoveDiceRequest(
                        Side.Enemy,
                        enemyInventory[_inventoryIndex - 1],
                        MoveDiceTarget.Board(new SlotId(Side.Enemy, _slotNumber)))
                };
            }
        }

        private sealed class ValidThenInvalidPlanner : IEnemySetupPlanner
        {
            public IReadOnlyList<MoveDiceRequest> CreatePlan(
                BattleState battleState,
                BoardState boardState,
                BattleInventoryState battleInventoryState)
            {
                IReadOnlyList<DiceInstanceId> enemyInventory = battleInventoryState.InventoryDiceIds(Side.Enemy);
                return new[]
                {
                    new MoveDiceRequest(
                        Side.Enemy,
                        enemyInventory[0],
                        MoveDiceTarget.Board(new SlotId(Side.Enemy, 1))),
                    new MoveDiceRequest(
                        Side.Player,
                        enemyInventory[1],
                        MoveDiceTarget.Board(new SlotId(Side.Player, 1)))
                };
            }
        }
    }
}
