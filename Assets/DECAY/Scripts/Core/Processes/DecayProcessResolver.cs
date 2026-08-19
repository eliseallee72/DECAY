using System;

namespace Decay
{
    /// <summary>
    /// Authoritative ordered DECAY calculation shared by committed execution and read-only prediction.
    /// It owns only ephemeral sequence bookkeeping such as pending WILLSAVEs; DecayResolver remains the
    /// per-pair rule calculator and no presentation system reproduces these rules.
    /// </summary>
    internal sealed class DecayProcessResolver
    {
        private readonly DecayResolver _pairResolver;

        internal DecayProcessResolver(DecayResolver pairResolver)
        {
            _pairResolver = pairResolver ?? throw new ArgumentNullException(nameof(pairResolver));
        }

        internal DecayPairDecision ResolveNext(DecayProcessState processState)
        {
            if (processState == null) throw new ArgumentNullException(nameof(processState));
            if (processState.IsComplete) throw new InvalidOperationException("DECAY process calculation is already complete.");

            DecaySaveToken? enemySave = processState.TryPeekNextSave(Side.Enemy, out DecaySaveToken e)
                ? e
                : (DecaySaveToken?)null;
            DecaySaveToken? playerSave = processState.TryPeekNextSave(Side.Player, out DecaySaveToken p)
                ? p
                : (DecaySaveToken?)null;

            DecayPairDecision decision = _pairResolver.ResolvePair(processState.CurrentPairId, enemySave, playerSave);
            processState.ApplyResolvedDecision(decision);
            return decision;
        }
    }
}
