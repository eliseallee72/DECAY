using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Coordinates battle presentation boundaries around already-authoritative results. It requests named editor-authored
    /// presentation from Views and advances BattleController only after required blocking presentation reports completion.
    /// It contains no gameplay rules and no authored visual sequences.
    /// </summary>
    public sealed class BattlePresentationDirector : MonoBehaviour
    {
        [Header("Editor Presentation Settings")]
        [SerializeField] private BattlePresentationSettings _settings = new BattlePresentationSettings();

        [Header("Scene Presentation Surfaces")]
        [SerializeField] private HourglassView _hourglassView;
        [SerializeField] private RoundCounterView _roundCounterView;
        [SerializeField] private BattleBoardView _boardView;

        private BattleCompositionRoot _compositionRoot;
        private BattleDiceViewCoordinator _diceViews;
        private BattleSceneDiceLayout _layout;
        private readonly List<DiceInstanceId> _activeSetupDiceIds = new List<DiceInstanceId>();
        private readonly List<DiceRolledFact> _activeRollFacts = new List<DiceRolledFact>();
        private PresentationCompletionBarrier _setupBarrier;
        private PresentationCompletionBarrier _rollBarrier;
        private PresentationCompletionBarrier _faceRevealBarrier;
        private PresentationCompletionBarrier _enemyRepositionBarrier;

        public bool IsBound => _compositionRoot != null;
        public BattlePresentationSettings Settings => _settings;

        public bool TryValidate(out string error)
        {
            if (_settings == null)
            {
                error = $"{name}: Battle Presentation Settings are missing.";
                return false;
            }

            if (!_settings.TryValidate(out error))
            {
                error = $"{name}: {error}";
                return false;
            }

            if (_hourglassView == null)
            {
                error = $"{name}: HourglassView is required for battle presentation flow.";
                return false;
            }

            if (!_hourglassView.TryValidate(out error))
                return false;
            if (_roundCounterView != null && !_roundCounterView.TryValidate(out error))
                return false;
            if (_boardView != null && !_boardView.TryValidate(out error))
                return false;

            error = string.Empty;
            return true;
        }

        internal void Bind(
            BattleCompositionRoot compositionRoot,
            BattleDiceViewCoordinator diceViews,
            BattleSceneDiceLayout layout)
        {
            _compositionRoot = compositionRoot ?? throw new ArgumentNullException(nameof(compositionRoot));
            _diceViews = diceViews ?? throw new ArgumentNullException(nameof(diceViews));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);

            _hourglassView.BindPresentationDirector(this);
        }

        internal void PresentEnemySetup(EnemySetupExecutionResult setupResult)
        {
            if (!IsBound) throw new InvalidOperationException("BattlePresentationDirector must be bound before presentation begins.");
            if (setupResult == null) throw new ArgumentNullException(nameof(setupResult));

            CancelEnemySetupPresentation();
            CollectSetupDice(setupResult, _activeSetupDiceIds);
            if (_activeSetupDiceIds.Count == 0)
                return;

            _setupBarrier = new PresentationCompletionBarrier(() =>
            {
                _setupBarrier = null;
                _activeSetupDiceIds.Clear();
            });

            for (int i = 0; i < _activeSetupDiceIds.Count; i++)
            {
                if (!_compositionRoot.TryGetDiceView(_activeSetupDiceIds[i], out DiceView view))
                    continue;
                Action completed = _setupBarrier.Register();
                view.PlayEnemySetupPresentation(completed);
            }
            _setupBarrier.Seal();
        }

        internal BattleFlowResult RequestRollFromHourglass()
        {
            RequireBound();
            CancelEnemySetupPresentation();
            CancelRollPresentation();

            BattleFlowResult result = _compositionRoot.Runtime.BattleController.RequestRoll();
            if (result.IsRejected)
                return result;

            _activeRollFacts.Clear();
            for (int i = 0; i < result.Facts.Count; i++)
            {
                if (result.Facts[i] is DiceRolledFact rolled)
                    _activeRollFacts.Add(rolled);
            }

            _rollBarrier = new PresentationCompletionBarrier(BeginFaceRevealPresentation);

            Action hourglassCompleted = _rollBarrier.Register();
            _hourglassView.PlayRollPresentation(hourglassCompleted);

            for (int i = 0; i < _activeRollFacts.Count; i++)
            {
                DiceRolledFact rolled = _activeRollFacts[i];
                if (!_compositionRoot.TryGetDiceView(rolled.DiceId, out DiceView view))
                    continue;
                Action diceCompleted = _rollBarrier.Register();
                view.PlayRollPresentation(diceCompleted);
            }

            if (_roundCounterView != null && _compositionRoot.Runtime.BattleState.CurrentRoundNumber == 1)
            {
                Action counterCompleted = _rollBarrier.Register();
                _roundCounterView.PlayShowRound(_compositionRoot.Runtime.BattleState.CurrentRoundNumber, counterCompleted);
            }

            _rollBarrier.Seal();
            return result;
        }

        internal void RequestDecayFromHourglass()
        {
            RequireBound();
            _diceViews.ClearPredictiveDecayPresentation();
            _hourglassView.AdvanceDecayImmediatelyToNextPlayableState();
        }

        internal void NotifyAuthoritativeBoardChanged()
        {
            if (!IsBound || _compositionRoot.Runtime.BattleState.CurrentPhase != BattlePhase.PlayerReposition)
                return;
            RefreshPredictiveDecayPresentation();
        }

        internal void CancelAllPresentation()
        {
            CancelEnemySetupPresentation();
            CancelRollPresentation();

            _faceRevealBarrier?.Cancel();
            _faceRevealBarrier = null;
            _enemyRepositionBarrier?.Cancel();
            _enemyRepositionBarrier = null;
            _boardView?.CancelAllPresentation();
            _roundCounterView?.CancelAllPresentation();
            _hourglassView?.CancelAllPresentation();
            _diceViews?.CancelAllPresentation();
        }

        private void BeginFaceRevealPresentation()
        {
            _rollBarrier = null;
            _faceRevealBarrier?.Cancel();
            _faceRevealBarrier = new PresentationCompletionBarrier(CompleteRollPresentation);

            for (int i = 0; i < _activeRollFacts.Count; i++)
            {
                DiceRolledFact rolled = _activeRollFacts[i];
                _diceViews.ReconcileDice(rolled.DiceId);

                if (_layout.TryGetSlotView(rolled.SlotId, out SlotView slotView)
                    && _compositionRoot.Runtime.BattleInventoryState.TryGetDice(rolled.DiceId, out DiceRuntimeState diceState))
                {
                    slotView.ShowScoreValue(diceState.ActiveScoreContribution);
                }

                if (_compositionRoot.TryGetDiceView(rolled.DiceId, out DiceView view))
                {
                    Action revealCompleted = _faceRevealBarrier.Register();
                    view.PlayFaceRevealPresentation(revealCompleted);
                }
            }

            _faceRevealBarrier.Seal();
        }

        private void CompleteRollPresentation()
        {
            _faceRevealBarrier = null;
            _activeRollFacts.Clear();

            BattleFlowResult completion = _compositionRoot.Runtime.BattleController.CompleteRoll();
            if (completion.IsRejected)
            {
                _compositionRoot.ReconcileViews();
                return;
            }

            BeginEnemyRepositionPresentation();
        }

        private void BeginEnemyRepositionPresentation()
        {
            _faceRevealBarrier?.Cancel();
            _faceRevealBarrier = null;
            _enemyRepositionBarrier?.Cancel();
            _enemyRepositionBarrier = new PresentationCompletionBarrier(CompleteEnemyRepositionPresentation);

            if (_boardView != null)
            {
                Action boardCompleted = _enemyRepositionBarrier.Register();
                _boardView.PlayEnemyRepositionPresentation(boardCompleted);
            }

            _enemyRepositionBarrier.Seal();
        }

        private void CompleteEnemyRepositionPresentation()
        {
            _enemyRepositionBarrier = null;
            BattleFlowResult completion = _compositionRoot.Runtime.BattleController.CompleteEnemyReposition();
            _compositionRoot.ReconcileViews();
            if (completion.IsApproved)
                RefreshPredictiveDecayPresentation();
        }

        private void RefreshPredictiveDecayPresentation()
        {
            if (_compositionRoot.Runtime.BattleState.CurrentPhase != BattlePhase.PlayerReposition)
            {
                _diceViews.ClearPredictiveDecayPresentation();
                return;
            }

            DecayPreviewResult preview = _compositionRoot.Runtime.BattleController.ResolveDecayPreview();
            _diceViews.ApplyPredictiveDecayPreview(preview);
        }

        private void CancelEnemySetupPresentation()
        {
            _setupBarrier?.Cancel();
            _setupBarrier = null;
            for (int i = 0; i < _activeSetupDiceIds.Count; i++)
            {
                if (_compositionRoot != null && _compositionRoot.TryGetDiceView(_activeSetupDiceIds[i], out DiceView view))
                    view.CancelEnemySetupPresentation();
            }
            _activeSetupDiceIds.Clear();
        }

        private void CancelRollPresentation()
        {
            _rollBarrier?.Cancel();
            _rollBarrier = null;
            _faceRevealBarrier?.Cancel();
            _faceRevealBarrier = null;
            for (int i = 0; i < _activeRollFacts.Count; i++)
            {
                if (_compositionRoot != null && _compositionRoot.TryGetDiceView(_activeRollFacts[i].DiceId, out DiceView view))
                {
                    view.CancelRollPresentation();
                    view.CancelFaceRevealPresentation();
                }
            }
            _activeRollFacts.Clear();
            _hourglassView?.CancelRollPresentation();
        }

        private static void CollectSetupDice(EnemySetupExecutionResult result, List<DiceInstanceId> output)
        {
            output.Clear();
            for (int i = 0; i < result.Movements.Count; i++)
            {
                BattleFact fact = result.Movements[i].Fact;
                if (fact is DicePlacedOnBoardFact placed)
                {
                    AddUnique(output, placed.DiceId);
                }
                else if (fact is DiceMovedOnBoardFact moved)
                {
                    AddUnique(output, moved.DiceId);
                }
                else if (fact is BoardDiceSwappedFact boardSwap)
                {
                    AddUnique(output, boardSwap.FirstDiceId);
                    AddUnique(output, boardSwap.SecondDiceId);
                }
                else if (fact is BoardInventoryDiceSwappedFact inventorySwap)
                {
                    AddUnique(output, inventorySwap.BoardToInventoryDiceId);
                    AddUnique(output, inventorySwap.InventoryToBoardDiceId);
                }
            }
        }

        private static void AddUnique(List<DiceInstanceId> output, DiceInstanceId diceId)
        {
            if (!diceId.IsValid || output.Contains(diceId))
                return;
            output.Add(diceId);
        }

        internal void ConfigureForTests(
            HourglassView hourglassView,
            RoundCounterView roundCounterView = null,
            BattleBoardView boardView = null,
            BattlePresentationSettings settings = null)
        {
            _hourglassView = hourglassView;
            _roundCounterView = roundCounterView;
            _boardView = boardView;
            if (settings != null)
                _settings = settings;
        }

        private void RequireBound()
        {
            if (!IsBound)
                throw new InvalidOperationException("BattlePresentationDirector is not bound to a battle runtime.");
        }

        private void OnEnable()
        {
            if (IsBound && _hourglassView != null)
                _hourglassView.BindPresentationDirector(this);
        }

        private void OnDisable()
        {
            CancelAllPresentation();
            _hourglassView?.UnbindPresentationDirector(this);
        }
    }
}
