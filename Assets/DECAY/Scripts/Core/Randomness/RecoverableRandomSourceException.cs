using System;

namespace Decay
{
    /// <summary>
    /// Signals that an injected random source could not provide an authored/runtime value, while the
    /// surrounding gameplay operation is still safe to retry from a different injected source before commit.
    /// This is intentionally narrow: invariant and programming errors must not be converted into random fallback.
    /// </summary>
    public sealed class RecoverableRandomSourceException : InvalidOperationException
    {
        public RecoverableRandomSourceException(string message)
            : base(message)
        {
        }
    }
}
