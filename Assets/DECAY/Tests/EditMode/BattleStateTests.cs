using NUnit.Framework;
using UnityEngine;

namespace Decay.Tests
{
    public sealed class BattleStateTests
    {
        [Test]
        public void BattleState_StartsAtGameOneRoundOneSetup()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);

            Assert.That(state.CurrentGameNumber, Is.EqualTo(1));
            Assert.That(state.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(state.IsBattleComplete, Is.False);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BattlePhaseController_ChangesAuthoritativePhaseAndRecordsFact()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            PhaseChangeResult result = controller.Handle(new PhaseChangeRequest(BattlePhase.Rolling));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(result.DenialReason, Is.EqualTo(PhaseChangeDenialReason.None));
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(result.Fact.SequenceNumber, Is.EqualTo(1));
            Assert.That(result.Fact.PreviousPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(result.Fact.CurrentPhase, Is.EqualTo(BattlePhase.Rolling));
            Assert.That(result.Fact.PreviousContext, Is.EqualTo(new BattleFactContext(1, 1, BattlePhase.Setup)));
            Assert.That(result.Fact.CurrentContext, Is.EqualTo(new BattleFactContext(1, 1, BattlePhase.Rolling)));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BattlePhaseController_RejectsSkippedPhaseWithoutChangingStateOrHistory()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            PhaseChangeResult result = controller.Handle(new PhaseChangeRequest(BattlePhase.DecayProcess));

            Assert.That(result.IsApproved, Is.False);
            Assert.That(result.DenialReason, Is.EqualTo(PhaseChangeDenialReason.TransitionNotAllowed));
            Assert.That(result.Fact, Is.Null);
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(history.Count, Is.Zero);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void RoundEndToSetup_IncrementsRoundWhenGameEndConditionIsNotMet()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            AdvanceThroughRound(controller, state);
            PhaseChangeResult result = controller.Handle(new PhaseChangeRequest(BattlePhase.Setup));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(state.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(state.CurrentGameNumber, Is.EqualTo(1));
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.Setup));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void RoundLimit_RequiresGameEndInsteadOfStartingExtraRound()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            AdvanceToConfiguredRoundLimit(controller, state);

            PhaseChangeResult rejected = controller.Handle(new PhaseChangeRequest(BattlePhase.Setup));
            PhaseChangeResult gameEnd = controller.Handle(new PhaseChangeRequest(BattlePhase.GameEnd));

            Assert.That(rejected.IsApproved, Is.False);
            Assert.That(rejected.DenialReason, Is.EqualTo(PhaseChangeDenialReason.RoundLimitRequiresGameEnd));
            Assert.That(gameEnd.IsApproved, Is.True);
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.GameEnd));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void RoundEndToGameEnd_IsRejectedBeforeAnyGameEndConditionIsMet()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            AdvanceThroughRound(controller, state);
            PhaseChangeResult result = controller.Handle(new PhaseChangeRequest(BattlePhase.GameEnd));

            Assert.That(result.IsApproved, Is.False);
            Assert.That(result.DenialReason, Is.EqualTo(PhaseChangeDenialReason.GameEndConditionNotMet));
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.RoundEnd));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void FullyBrokenPlayerSide_RequiresGameEndAfterScoring()
        {
            AssertBrokenSideRequiresGameEnd(Side.Player);
        }

        [Test]
        public void FullyBrokenEnemySide_RequiresGameEndAfterScoring()
        {
            AssertBrokenSideRequiresGameEnd(Side.Enemy);
        }

        [Test]
        public void GameEndToSetup_StartsNextGameAndResetsRoundNumber()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            AdvanceToConfiguredRoundLimit(controller, state);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.GameEnd)).IsApproved, Is.True);

            PhaseChangeResult nextGame = controller.Handle(new PhaseChangeRequest(BattlePhase.Setup));

            Assert.That(nextGame.IsApproved, Is.True);
            Assert.That(nextGame.Fact.PreviousContext, Is.EqualTo(new BattleFactContext(1, config.RoundsPerGame, BattlePhase.GameEnd)));
            Assert.That(nextGame.Fact.CurrentContext, Is.EqualTo(new BattleFactContext(2, 1, BattlePhase.Setup)));
            Assert.That(state.CurrentGameNumber, Is.EqualTo(2));
            Assert.That(state.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.Setup));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BattleEnd_IsOnlyApprovedAfterConfiguredFinalGame()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            AdvanceToConfiguredRoundLimit(controller, state);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.GameEnd)).IsApproved, Is.True);

            PhaseChangeResult tooEarly = controller.Handle(new PhaseChangeRequest(BattlePhase.BattleEnd));
            Assert.That(tooEarly.IsApproved, Is.False);
            Assert.That(tooEarly.DenialReason, Is.EqualTo(PhaseChangeDenialReason.MoreGamesRemain));

            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.Setup)).IsApproved, Is.True);
            AdvanceToConfiguredRoundLimit(controller, state);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.GameEnd)).IsApproved, Is.True);

            PhaseChangeResult battleEnd = controller.Handle(new PhaseChangeRequest(BattlePhase.BattleEnd));
            Assert.That(battleEnd.IsApproved, Is.True);
            Assert.That(state.IsBattleComplete, Is.True);

            PhaseChangeResult afterComplete = controller.Handle(new PhaseChangeRequest(BattlePhase.Setup));
            Assert.That(afterComplete.IsApproved, Is.False);
            Assert.That(afterComplete.DenialReason, Is.EqualTo(PhaseChangeDenialReason.BattleAlreadyComplete));

            Object.DestroyImmediate(config);
        }

        private static BattlePhaseController CreateController(BattleState state, BoardState board, BattleHistory history)
        {
            return new BattlePhaseController(state, board, new BattlePhaseTransitionValidator(), history);
        }

        private static void AdvanceToConfiguredRoundLimit(BattlePhaseController controller, BattleState state)
        {
            while (true)
            {
                AdvanceThroughRound(controller, state);
                if (state.CurrentRoundNumber >= state.RoundsPerGame)
                {
                    return;
                }

                Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.Setup)).IsApproved, Is.True);
            }
        }

        private static void AdvanceThroughRound(BattlePhaseController controller, BattleState state)
        {
            AdvanceToScoreProcess(controller, state);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.RoundEnd)).IsApproved, Is.True);
        }

        private static void AdvanceToScoreProcess(BattlePhaseController controller, BattleState state)
        {
            Assert.That(state.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.Rolling)).IsApproved, Is.True);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.EnemyReposition)).IsApproved, Is.True);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.PlayerReposition)).IsApproved, Is.True);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.DecayProcess)).IsApproved, Is.True);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.ScoreProcess)).IsApproved, Is.True);
        }

        private static void AssertBrokenSideRequiresGameEnd(Side side)
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            var state = new BattleState(config);
            var board = new BoardState();
            var history = new BattleHistory();
            var controller = CreateController(state, board, history);

            AdvanceToScoreProcess(controller, state);
            BreakAllSlots(state, board, side);
            Assert.That(controller.Handle(new PhaseChangeRequest(BattlePhase.RoundEnd)).IsApproved, Is.True);

            PhaseChangeResult setup = controller.Handle(new PhaseChangeRequest(BattlePhase.Setup));
            PhaseChangeResult gameEnd = controller.Handle(new PhaseChangeRequest(BattlePhase.GameEnd));

            Assert.That(setup.IsApproved, Is.False);
            Assert.That(setup.DenialReason, Is.EqualTo(PhaseChangeDenialReason.BoardBreakRequiresGameEnd));
            Assert.That(gameEnd.IsApproved, Is.True);

            Object.DestroyImmediate(config);
        }

        private static void BreakAllSlots(BattleState state, BoardState board, Side side)
        {
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                new SetSlotConditionCommand(state, board, new SlotId(side, number), SlotCondition.Broken).Execute();
            }
        }
    }
}
