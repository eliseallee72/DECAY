
using UnityEngine;

namespace Decay
{
    public abstract class EffectDefinition : ScriptableObject
    {
        [SerializeField] private EffectId _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        public EffectId Id => _id;
        public string DisplayName => _displayName ?? string.Empty;
        public string Description => _description ?? string.Empty;

        public bool TryValidate(out string error)
        {
            if (!_id.IsValid)
            {
                error = $"{name}: effect ID is required and must use the effect.name format.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                error = $"{name}: display name is required.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void ConfigureForTests(EffectId id, string displayName, string description = "")
        {
            _id = id;
            _displayName = displayName;
            _description = description;
        }
    }
}
