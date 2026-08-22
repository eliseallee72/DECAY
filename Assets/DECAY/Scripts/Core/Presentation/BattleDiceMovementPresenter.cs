using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Presentation-only motion for already-approved dice movement. Gameplay owns the semantic destination;
    /// BattleDiceViewCoordinator resolves that destination to serialized scene anchors, then this presenter moves
    /// the currently rendered transform toward that resolved target using only editor-authored duration/easing.
    /// </summary>
    internal sealed class BattleDiceMovementPresenter
    {
        private readonly MonoBehaviour _coroutineHost;
        private readonly BattleDiceViewCoordinator _diceViews;
        private readonly BattlePresentationSettings _settings;
        private readonly Dictionary<DiceView, Coroutine> _activeMotions = new Dictionary<DiceView, Coroutine>();
        private readonly HashSet<DiceView> _activePresentations = new HashSet<DiceView>();

        internal BattleDiceMovementPresenter(
            MonoBehaviour coroutineHost,
            BattleDiceViewCoordinator diceViews,
            BattlePresentationSettings settings)
        {
            _coroutineHost = coroutineHost ?? throw new ArgumentNullException(nameof(coroutineHost));
            _diceViews = diceViews ?? throw new ArgumentNullException(nameof(diceViews));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal bool HasActiveMotion => _activePresentations.Count > 0;

        internal bool TryPresent(MoveDiceResult result, Action onCompleted = null)
        {
            if (result == null || !result.IsApproved)
                return false;

            switch (result.Fact)
            {
                case BoardDiceSwappedFact swap:
                    _diceViews.RefreshAllDestinations();
                    PresentSwap(swap, onCompleted);
                    return true;

                case DiceMovedOnBoardFact moved:
                    _diceViews.RefreshAllDestinations();
                    PresentSingle(moved.DiceId, _settings.BoardSwap, onCompleted);
                    return true;

                case DicePlacedOnBoardFact placed:
                    _diceViews.RefreshAllDestinations();
                    PresentSingle(placed.DiceId, _settings.DiceSettle, onCompleted);
                    return true;

                default:
                    return false;
            }
        }

        internal void CancelAndReconcile()
        {
            if (_activePresentations.Count > 0)
            {
                var views = new List<DiceView>(_activePresentations);
                for (int i = 0; i < views.Count; i++)
                    CancelPresentation(views[i]);
            }

            _diceViews.ReconcileAll(false);
        }

        private void PresentSwap(BoardDiceSwappedFact swap, Action onCompleted)
        {
            var barrier = new PresentationCompletionBarrier(onCompleted ?? (() => { }));
            PresentSingle(swap.FirstDiceId, _settings.BoardSwap, barrier.Register());
            PresentSingle(swap.SecondDiceId, _settings.BoardSwap, barrier.Register());
            barrier.Seal();
        }

        private void PresentSingle(
            DiceInstanceId diceId,
            BattlePresentationSettings.CodedMotionSettings motionSettings,
            Action onCompleted)
        {
            if (!_diceViews.TryGetView(diceId, out DiceView view))
            {
                onCompleted?.Invoke();
                return;
            }

            CancelPresentation(view);
            _activePresentations.Add(view);
            Action completed = () =>
            {
                _activeMotions.Remove(view);
                _activePresentations.Remove(view);
                onCompleted?.Invoke();
            };

            if (motionSettings == null || !motionSettings.IsConfigured)
            {
                view.ReconcileRenderedTransformToDestination();
                view.PlaySettlePresentation(completed);
                return;
            }

            Vector3 startWorldPosition = view.transform.position;
            Coroutine routine = _coroutineHost.StartCoroutine(MoveToDestination(
                view,
                startWorldPosition,
                motionSettings,
                completed));
            _activeMotions[view] = routine;
        }

        private IEnumerator MoveToDestination(
            DiceView view,
            Vector3 startWorldPosition,
            BattlePresentationSettings.CodedMotionSettings motionSettings,
            Action onCompleted)
        {
            float elapsed = 0f;
            while (elapsed < motionSettings.Duration)
            {
                if (view == null)
                {
                    onCompleted?.Invoke();
                    yield break;
                }

                float normalizedTime = Mathf.Clamp01(elapsed / motionSettings.Duration);
                float progress = motionSettings.Easing.Evaluate(normalizedTime);
                view.SetPreviewWorldPosition(Vector3.LerpUnclamped(
                    startWorldPosition,
                    view.PresentationDestinationWorldPosition,
                    progress));

                elapsed += motionSettings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            if (view == null)
            {
                onCompleted?.Invoke();
                yield break;
            }

            view.ReconcileRenderedTransformToDestination();
            _activeMotions.Remove(view);
            view.PlaySettlePresentation(onCompleted);
        }

        private void CancelPresentation(DiceView view)
        {
            if (view == null || !_activePresentations.Contains(view))
                return;

            if (_activeMotions.TryGetValue(view, out Coroutine routine) && routine != null)
                _coroutineHost.StopCoroutine(routine);

            _activeMotions.Remove(view);
            _activePresentations.Remove(view);
            view.CancelAllPresentation();
        }
    }
}
