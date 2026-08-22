using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Coordinates presentation around already-authoritative battle results. It never requests or completes gameplay
    /// phases. The authoritative Unity bridge supplies results and completion callbacks; this Director only displays
    /// those results and reports when blocking presentation has completed.
    /// </summary>
    public sealed class BattlePresentationDirector : MonoBehaviour
    {
        [Header("Editor Presentation Settings")]
        [SerializeField] private BattlePresentationSettings _settings = new BattlePresentationSettings();

        [Header("Scene Presentation Surfaces")]
        [SerializeField] private HourglassView _hourglassView;
        [SerializeField] private RoundCounterView _roundCounterView;
        [SerializeField] private BattleBoardView _boardView;

        [Header("Optional Ambient Presentation Surfaces")]
        [Tooltip("Optional enemy presentation surface. Assign the enemy object here when its authored Idle/hover animation is ready.")]
        [SerializeField] private AmbientPresentationView _enemyAmbientView;
        [Tooltip("Optional abacus presentation surface. Assign the abacus object here when its authored Idle/hover animation is ready.")]
        [SerializeField] private AmbientPresentationView _abacusAmbientView;

        private BattleRuntime _runtime;
        private BattleDiceViewCoordinator _diceViews;
        private BattleSceneDiceLayout _layout;
        private Action _hourglassInteractionRequested;
        private Action _rollPresentationCompleted;
        private Action _enemyRepositionPresentationCompleted;
        private Action _enemySetupPresentationCompleted;
        private readonly List<DiceInstanceId> _activeSetupDiceIds = new List<DiceInstanceId>();
        private readonly List<Coroutine> _activeSetupStartCoroutines = new List<Coroutine>();
        private readonly List<DiceRolledFact> _activeRollFacts = new List<DiceRolledFact>();
        private readonly List<Coroutine> _activeRollStartCoroutines = new List<Coroutine>();
        private readonly System.Random _presentationRandom = new System.Random();
        private PresentationCompletionBarrier _setupBarrier;
        private PresentationCompletionBarrier _rollBarrier;
        private PresentationCompletionBarrier _faceRevealBarrier;
        private PresentationCompletionBarrier _enemyRepositionBarrier;
        private int _enemySetupGeneration;
        private int _rollGeneration;

        public bool IsBound => _runtime != null;
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
            if (!_hourglassView.TryValidate(out error)) return false;
            if (_roundCounterView != null && !_roundCounterView.TryValidate(out error)) return false;
            if (_boardView != null && !_boardView.TryValidate(out error)) return false;
            if (_enemyAmbientView != null && !_enemyAmbientView.TryValidate(out error)) return false;
            if (_abacusAmbientView != null && !_abacusAmbientView.TryValidate(out error)) return false;

            error = string.Empty;
            return true;
        }

        internal void Bind(
            BattleRuntime runtime,
            BattleDiceViewCoordinator diceViews,
            BattleSceneDiceLayout layout,
            Action hourglassInteractionRequested)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _diceViews = diceViews ?? throw new ArgumentNullException(nameof(diceViews));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _hourglassInteractionRequested = hourglassInteractionRequested ?? throw new ArgumentNullException(nameof(hourglassInteractionRequested));

            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);

            _hourglassView.BindInteraction(_hourglassInteractionRequested);
            ReconcileAuthoritativeState();
        }

        internal void PresentEnemySetup(EnemySetupExecutionResult setupResult, Action onCompleted = null)
        {
            RequireBound();
            if (setupResult == null) throw new ArgumentNullException(nameof(setupResult));

            CancelEnemySetupPresentation();
            int generation = _enemySetupGeneration;
            _enemySetupPresentationCompleted = onCompleted;
            CollectSetupDice(setupResult, _activeSetupDiceIds);
            if (_activeSetupDiceIds.Count == 0)
            {
                CompleteEnemySetupPresentation();
                return;
            }

            _setupBarrier = new PresentationCompletionBarrier(CompleteEnemySetupPresentation);
            int presentedIndex = 0;
            for (int i = 0; i < _activeSetupDiceIds.Count; i++)
            {
                if (!_diceViews.TryGetView(_activeSetupDiceIds[i], out DiceView view))
                    continue;

                Action completed = _setupBarrier.Register();
                float startDelay = _settings.EnemySetupStartStagger * presentedIndex;
                presentedIndex++;

                if (startDelay <= 0f)
                {
                    view.PlayEnemySetupPresentation(completed);
                    continue;
                }

                Coroutine routine = StartCoroutine(StartEnemySetupPresentationAfterDelay(
                    view,
                    startDelay,
                    generation,
                    completed));
                _activeSetupStartCoroutines.Add(routine);
            }
            _setupBarrier.Seal();
        }

        internal void PresentRoll(BattleFlowResult authoritativeRollResult, Action onCompleted)
        {
            RequireBound();
            if (authoritativeRollResult == null) throw new ArgumentNullException(nameof(authoritativeRollResult));
            if (!authoritativeRollResult.IsApproved)
                throw new ArgumentException("Presentation may only display an approved authoritative Roll result.", nameof(authoritativeRollResult));

            // Enemy setup authority already committed its semantic destinations before setup presentation began.
            // Interrupt only its transient visuals and rendered transforms; do not refresh visual content here because
            // Roll authority has already selected faces that must remain hidden until face-reveal presentation.
            CancelEnemySetupPresentation(reconcileRenderedDestinations: true);
            CancelRollPresentation();
            int generation = _rollGeneration;
            _rollPresentationCompleted = onCompleted;
            _activeRollFacts.Clear();
            for (int i = 0; i < authoritativeRollResult.Facts.Count; i++)
            {
                if (authoritativeRollResult.Facts[i] is DiceRolledFact rolled)
                    _activeRollFacts.Add(rolled);
            }

            _hourglassView.ReconcilePhase(_runtime.BattleState.CurrentPhase);
            _hourglassView.SetPresentationInteractionEnabled(false);
            SetAmbientIdlePresentation(false);
            _rollBarrier = new PresentationCompletionBarrier(BeginFaceRevealPresentation);

            Action hourglassCompleted = _rollBarrier.Register();
            _hourglassView.PlayRollPresentation(hourglassCompleted);

            for (int i = 0; i < _activeRollFacts.Count; i++)
            {
                if (!_diceViews.TryGetView(_activeRollFacts[i].DiceId, out DiceView view))
                    continue;

                Action diceCompleted = _rollBarrier.Register();
                float startDelay = NextRollStartDelay();
                if (startDelay <= 0f)
                {
                    view.PlayRollPresentation(diceCompleted);
                    continue;
                }

                Coroutine routine = StartCoroutine(StartRollPresentationAfterDelay(
                    view,
                    startDelay,
                    generation,
                    diceCompleted));
                _activeRollStartCoroutines.Add(routine);
            }

            if (_roundCounterView != null && _runtime.BattleState.CurrentRoundNumber == 1)
            {
                Action counterCompleted = _rollBarrier.Register();
                _roundCounterView.PlayShowRound(_runtime.BattleState.CurrentRoundNumber, counterCompleted);
            }

            _rollBarrier.Seal();
        }

        internal void PresentEnemyReposition(Action onCompleted)
        {
            RequireBound();
            _hourglassView.SetPresentationInteractionEnabled(false);
            SetAmbientIdlePresentation(false);
            _enemyRepositionPresentationCompleted = onCompleted;
            _enemyRepositionBarrier?.Cancel();
            _enemyRepositionBarrier = new PresentationCompletionBarrier(CompleteEnemyRepositionPresentation);

            if (_boardView != null)
            {
                Action boardCompleted = _enemyRepositionBarrier.Register();
                _boardView.PlayEnemyRepositionPresentation(boardCompleted);
            }
            _enemyRepositionBarrier.Seal();
        }

        internal void PresentPredictiveDecayPreview(DecayPreviewResult preview)
        {
            RequireBound();
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            _diceViews.ApplyPredictiveDecayPreview(preview);
        }

        internal void ClearPredictiveDecayPresentation()
        {
            _diceViews?.ClearPredictiveDecayPresentation();
        }

        /// <summary>
        /// Skips the currently blocking presentation, reconciles to the authoritative state that already exists,
        /// then reports presentation completion through the callback supplied by the authoritative bridge.
        /// This is the interruption path for visuals that are explicitly allowed to be skipped.
        /// </summary>
        internal bool SkipActivePresentationAndReconcile()
        {
            Action continuation = _rollPresentationCompleted
                ?? _enemyRepositionPresentationCompleted
                ?? _enemySetupPresentationCompleted;

            CancelTransientPresentation();
            ReconcileAuthoritativeState(true);
            continuation?.Invoke();
            return continuation != null;
        }

        /// <summary>
        /// Cancels transient presentation and falls back to current authoritative persistent state without reporting
        /// completion. Use this only when the owning battle flow is itself being abandoned/restarted/unloaded.
        /// </summary>
        internal void CancelAndReconcile()
        {
            CancelTransientPresentation();
            ReconcileAuthoritativeState(true);
        }

        internal void ReconcileAuthoritativeState(bool invokeRecoveryHooks = false)
        {
            if (!IsBound) return;

            BattlePhase phase = _runtime.BattleState.CurrentPhase;
            bool passiveInteractionPhase = IsPassiveInteractionPhase(phase);

            _diceViews.ReconcileAll(invokeRecoveryHooks);
            _hourglassView.ReconcilePhase(phase, invokeRecoveryHooks);
            _hourglassView.SetPresentationInteractionEnabled(passiveInteractionPhase);
            _roundCounterView?.ReconcileRoundNumber(_runtime.BattleState.CurrentRoundNumber, invokeRecoveryHooks);
            _boardView?.ReconcileAuthoritativePresentation(invokeRecoveryHooks);
            SetAmbientIdlePresentation(passiveInteractionPhase);

            if (phase == BattlePhase.PlayerReposition)
                _diceViews.ApplyPredictiveDecayPreview(_runtime.BattleController.ResolveDecayPreview());
            else
                _diceViews.ClearPredictiveDecayPresentation();
        }

        private IEnumerator StartEnemySetupPresentationAfterDelay(
            DiceView view,
            float delay,
            int generation,
            Action onCompleted)
        {
            yield return new WaitForSeconds(delay);

            if (generation != _enemySetupGeneration)
                yield break;

            if (view == null)
            {
                onCompleted?.Invoke();
                yield break;
            }

            view.PlayEnemySetupPresentation(onCompleted);
        }

        private IEnumerator StartRollPresentationAfterDelay(
            DiceView view,
            float delay,
            int generation,
            Action onCompleted)
        {
            yield return new WaitForSeconds(delay);

            if (generation != _rollGeneration)
                yield break;

            if (view == null)
            {
                onCompleted?.Invoke();
                yield break;
            }

            view.PlayRollPresentation(onCompleted);
        }

        private float NextRollStartDelay()
        {
            Vector2 range = _settings.RollStartOffsetRange;
            if (range.y <= range.x)
                return range.x;

            return range.x + ((float)_presentationRandom.NextDouble() * (range.y - range.x));
        }

        private void BeginFaceRevealPresentation()
        {
            _rollBarrier = null;
            _activeRollStartCoroutines.Clear();
            _faceRevealBarrier?.Cancel();
            _faceRevealBarrier = new PresentationCompletionBarrier(CompleteRollPresentation);

            for (int i = 0; i < _activeRollFacts.Count; i++)
            {
                DiceRolledFact rolled = _activeRollFacts[i];
                // Update authoritative content/destination without snapping the rendered transform. The Roll animation
                // owns what is visible until its authored completion, while gameplay state is already resolved.
                _diceViews.ReconcileDiceVisualState(rolled.DiceId);

                if (_layout.TryGetSlotView(rolled.SlotId, out SlotView slotView)
                    && _runtime.BattleInventoryState.TryGetDice(rolled.DiceId, out DiceRuntimeState diceState))
                {
                    slotView.ShowScoreValue(diceState.ActiveScoreContribution);
                }

                if (_diceViews.TryGetView(rolled.DiceId, out DiceView view))
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
            _activeRollStartCoroutines.Clear();
            _activeRollFacts.Clear();
            Action callback = _rollPresentationCompleted;
            _rollPresentationCompleted = null;
            callback?.Invoke();
        }

        private void CompleteEnemyRepositionPresentation()
        {
            _enemyRepositionBarrier = null;
            Action callback = _enemyRepositionPresentationCompleted;
            _enemyRepositionPresentationCompleted = null;
            callback?.Invoke();
        }

        private void CompleteEnemySetupPresentation()
        {
            _setupBarrier = null;
            _activeSetupStartCoroutines.Clear();
            _activeSetupDiceIds.Clear();
            Action callback = _enemySetupPresentationCompleted;
            _enemySetupPresentationCompleted = null;
            callback?.Invoke();
        }

        private void CancelTransientPresentation()
        {
            CancelEnemySetupPresentation();
            CancelRollPresentation();
            _enemyRepositionBarrier?.Cancel();
            _enemyRepositionBarrier = null;
            _enemyRepositionPresentationCompleted = null;
            _boardView?.CancelAllPresentation();
            _roundCounterView?.CancelAllPresentation();
            _hourglassView?.CancelAllPresentation();
            _diceViews?.CancelAllPresentation();
        }

        private void CancelEnemySetupPresentation(bool reconcileRenderedDestinations = false)
        {
            _enemySetupGeneration++;
            for (int i = 0; i < _activeSetupStartCoroutines.Count; i++)
            {
                if (_activeSetupStartCoroutines[i] != null)
                    StopCoroutine(_activeSetupStartCoroutines[i]);
            }
            _activeSetupStartCoroutines.Clear();

            _setupBarrier?.Cancel();
            _setupBarrier = null;
            _enemySetupPresentationCompleted = null;
            for (int i = 0; i < _activeSetupDiceIds.Count; i++)
            {
                if (_diceViews == null || !_diceViews.TryGetView(_activeSetupDiceIds[i], out DiceView view))
                    continue;

                view.CancelEnemySetupPresentation();
                if (reconcileRenderedDestinations)
                    view.ReconcileRenderedTransformToDestination();
            }
            _activeSetupDiceIds.Clear();
        }

        private void CancelRollPresentation()
        {
            _rollGeneration++;
            for (int i = 0; i < _activeRollStartCoroutines.Count; i++)
            {
                if (_activeRollStartCoroutines[i] != null)
                    StopCoroutine(_activeRollStartCoroutines[i]);
            }
            _activeRollStartCoroutines.Clear();

            _rollBarrier?.Cancel();
            _rollBarrier = null;
            _faceRevealBarrier?.Cancel();
            _faceRevealBarrier = null;
            _rollPresentationCompleted = null;
            for (int i = 0; i < _activeRollFacts.Count; i++)
            {
                if (_diceViews != null && _diceViews.TryGetView(_activeRollFacts[i].DiceId, out DiceView view))
                {
                    view.CancelRollPresentation();
                    view.CancelFaceRevealPresentation();
                }
            }
            _activeRollFacts.Clear();
            _hourglassView?.CancelRollPresentation();
        }

        private void SetAmbientIdlePresentation(bool isActive)
        {
            _roundCounterView?.SetIdlePresentation(isActive);
            _enemyAmbientView?.SetIdlePresentation(isActive);
            _abacusAmbientView?.SetIdlePresentation(isActive);
        }

        private static bool IsPassiveInteractionPhase(BattlePhase phase) =>
            phase == BattlePhase.Setup || phase == BattlePhase.PlayerReposition;

        private static void CollectSetupDice(EnemySetupExecutionResult result, List<DiceInstanceId> output)
        {
            output.Clear();
            for (int i = 0; i < result.Movements.Count; i++)
            {
                BattleFact fact = result.Movements[i].Fact;
                if (fact is DicePlacedOnBoardFact placed) AddUnique(output, placed.DiceId);
                else if (fact is DiceMovedOnBoardFact moved) AddUnique(output, moved.DiceId);
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
            if (diceId.IsValid && !output.Contains(diceId)) output.Add(diceId);
        }

        internal void ConfigureForTests(
            HourglassView hourglassView,
            RoundCounterView roundCounterView = null,
            BattleBoardView boardView = null,
            BattlePresentationSettings settings = null,
            AmbientPresentationView enemyAmbientView = null,
            AmbientPresentationView abacusAmbientView = null)
        {
            _hourglassView = hourglassView;
            _roundCounterView = roundCounterView;
            _boardView = boardView;
            _enemyAmbientView = enemyAmbientView;
            _abacusAmbientView = abacusAmbientView;
            if (settings != null) _settings = settings;
        }

        private void RequireBound()
        {
            if (!IsBound)
                throw new InvalidOperationException("BattlePresentationDirector is not bound to a battle runtime.");
        }

        private void OnEnable()
        {
            if (IsBound && _hourglassView != null && _hourglassInteractionRequested != null)
            {
                _hourglassView.BindInteraction(_hourglassInteractionRequested);
                ReconcileAuthoritativeState(true);
            }
        }

        private void OnDisable()
        {
            if (_hourglassView != null && _hourglassInteractionRequested != null)
                _hourglassView.UnbindInteraction(_hourglassInteractionRequested);
            CancelTransientPresentation();
        }
    }
}
