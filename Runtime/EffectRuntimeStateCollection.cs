
using System;
using System.Collections.Generic;

namespace Decay
{
    public sealed class EffectRuntimeStateCollection
    {
        private readonly Dictionary<EffectInstanceId, IEffectRuntimeState> _byInstanceId = new Dictionary<EffectInstanceId, IEffectRuntimeState>();

        public int Count => _byInstanceId.Count;

        public bool TryGet(EffectInstanceId instanceId, out IEffectRuntimeState state)
        {
            return _byInstanceId.TryGetValue(instanceId, out state);
        }

        public void Register(IEffectRuntimeState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!state.InstanceId.IsValid)
            {
                throw new ArgumentException("Effect runtime state requires a valid effect instance ID.", nameof(state));
            }

            if (!state.EffectId.IsValid)
            {
                throw new ArgumentException("Effect runtime state requires a valid effect definition ID.", nameof(state));
            }

            if (_byInstanceId.ContainsKey(state.InstanceId))
            {
                throw new InvalidOperationException($"Runtime effect instance '{state.InstanceId}' is already registered.");
            }

            _byInstanceId.Add(state.InstanceId, state);
        }

        public void Clear()
        {
            _byInstanceId.Clear();
        }
    }
}
