using System;
using System.Collections.Generic;

namespace Decay
{
    internal sealed class MoveDiceGateCollection
    {
        private readonly IReadOnlyList<IMoveDiceGate> _gates;

        internal MoveDiceGateCollection(IReadOnlyList<IMoveDiceGate> gates)
        {
            _gates = gates ?? throw new ArgumentNullException(nameof(gates));
        }

        internal MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            for (int i = 0; i < _gates.Count; i++)
            {
                IMoveDiceGate gate = _gates[i] ?? throw new InvalidOperationException("Move dice Gate collection cannot contain null Gates.");
                MoveDiceDenialReason denial = gate.Evaluate(context);
                if (denial != MoveDiceDenialReason.None)
                {
                    return denial;
                }
            }

            return MoveDiceDenialReason.None;
        }
    }
}
