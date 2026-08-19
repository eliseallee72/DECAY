using System;

namespace Decay
{
    /// <summary>
    /// Process-local completion barrier for a bounded set of blocking presentation requests.
    /// It is deliberately not a general event queue: it only answers when every registered visual has reported completion.
    /// </summary>
    internal sealed class PresentationCompletionBarrier
    {
        private readonly Action _onCompleted;
        private int _pendingCount;
        private bool _isSealed;
        private bool _isCancelled;
        private bool _hasCompleted;

        internal PresentationCompletionBarrier(Action onCompleted)
        {
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
        }

        internal Action Register()
        {
            if (_isSealed)
            {
                throw new InvalidOperationException("Cannot register presentation after the completion barrier has been sealed.");
            }

            if (_isCancelled || _hasCompleted)
            {
                throw new InvalidOperationException("Cannot register presentation on an inactive completion barrier.");
            }

            _pendingCount++;
            bool callbackUsed = false;
            return () =>
            {
                if (callbackUsed || _isCancelled || _hasCompleted)
                {
                    return;
                }

                callbackUsed = true;
                _pendingCount--;
                TryComplete();
            };
        }

        internal void Seal()
        {
            if (_isCancelled || _hasCompleted)
            {
                return;
            }

            _isSealed = true;
            TryComplete();
        }

        internal void Cancel()
        {
            _isCancelled = true;
        }

        private void TryComplete()
        {
            if (!_isSealed || _pendingCount != 0 || _isCancelled || _hasCompleted)
            {
                return;
            }

            _hasCompleted = true;
            _onCompleted();
        }
    }
}
