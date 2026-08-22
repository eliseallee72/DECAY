using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Runtime-only composition of an optional authored Animator response and an optional editor-authored
    /// procedural transform response. Either layer may be used alone. When both are configured, blocking
    /// presentation completes only after both have reported completion.
    /// </summary>
    internal sealed class HybridOneShotPresentationRun
    {
        private readonly AnimatorTriggerPresentationBinding _authoredPresentation;
        private readonly ProceduralTransformPresentationBinding _proceduralPresentation;
        private readonly PresentationCompletionBarrier _completionBarrier;
        private Action _authoredCompletion;
        private bool _cancelled;

        private HybridOneShotPresentationRun(
            AnimatorTriggerPresentationBinding authoredPresentation,
            ProceduralTransformPresentationBinding proceduralPresentation,
            Action onCompleted)
        {
            _authoredPresentation = authoredPresentation;
            _proceduralPresentation = proceduralPresentation;
            _completionBarrier = new PresentationCompletionBarrier(onCompleted ?? (() => { }));
        }

        internal static HybridOneShotPresentationRun Start(
            MonoBehaviour owner,
            AnimatorTriggerPresentationBinding authoredPresentation,
            ProceduralTransformPresentationBinding proceduralPresentation,
            Action onCompleted)
        {
            bool hasAuthored = authoredPresentation != null && authoredPresentation.IsConfigured;
            bool hasProcedural = proceduralPresentation != null && proceduralPresentation.IsConfigured;

            if (!hasAuthored && !hasProcedural)
            {
                onCompleted?.Invoke();
                return null;
            }

            var run = new HybridOneShotPresentationRun(authoredPresentation, proceduralPresentation, onCompleted);

            if (hasAuthored)
            {
                run._authoredCompletion = run._completionBarrier.Register();
                if (!authoredPresentation.Play())
                {
                    Action authoredCompletion = run._authoredCompletion;
                    run._authoredCompletion = null;
                    authoredCompletion?.Invoke();
                }
            }

            if (hasProcedural)
            {
                Action proceduralCompletion = run._completionBarrier.Register();
                if (!proceduralPresentation.Play(owner, proceduralCompletion))
                    proceduralCompletion();
            }

            run._completionBarrier.Seal();
            return run;
        }

        internal void NotifyAuthoredComplete()
        {
            if (_cancelled)
                return;

            Action completion = _authoredCompletion;
            _authoredCompletion = null;
            completion?.Invoke();
        }

        internal void Cancel()
        {
            if (_cancelled)
                return;

            _cancelled = true;
            _completionBarrier.Cancel();
            _authoredCompletion = null;
            _authoredPresentation?.Cancel();
            _proceduralPresentation?.Cancel();
        }
    }
}
