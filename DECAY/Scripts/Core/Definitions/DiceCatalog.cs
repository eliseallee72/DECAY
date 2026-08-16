using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    [CreateAssetMenu(fileName = "catalog_DICE", menuName = "DECAY/Dice/Dice Catalog")]
    public sealed class DiceCatalog : ScriptableObject
    {
        [SerializeField] private List<DiceDefinition> _definitions = new List<DiceDefinition>();

        [NonSerialized] private Dictionary<DiceId, DiceDefinition> _byId;
        [NonSerialized] private bool _indexIsDirty = true;
        [NonSerialized] private string _indexError = string.Empty;

        public IReadOnlyList<DiceDefinition> Definitions => _definitions;

        public bool TryGet(DiceId id, out DiceDefinition definition)
        {
            definition = null;
            if (!id.IsValid || !EnsureIndex())
            {
                return false;
            }

            return _byId.TryGetValue(id, out definition);
        }

        public DiceDefinition GetRequired(DiceId id)
        {
            if (!EnsureIndex())
            {
                throw new InvalidOperationException(_indexError);
            }

            if (!_byId.TryGetValue(id, out DiceDefinition definition))
            {
                throw new KeyNotFoundException($"Dice ID '{id}' is not present in catalog '{name}'.");
            }

            return definition;
        }

        public bool TryValidate(out string error)
        {
            _indexIsDirty = true;
            bool valid = EnsureIndex();
            error = valid ? string.Empty : _indexError;
            return valid;
        }

        internal void ConfigureForTests(IEnumerable<DiceDefinition> definitions)
        {
            _definitions = definitions == null
                ? new List<DiceDefinition>()
                : new List<DiceDefinition>(definitions);
            _indexIsDirty = true;
        }

        private void OnEnable()
        {
            _indexIsDirty = true;
        }

        private void OnValidate()
        {
            _indexIsDirty = true;
        }

        private bool EnsureIndex()
        {
            if (!_indexIsDirty)
            {
                return string.IsNullOrEmpty(_indexError);
            }

            _indexIsDirty = false;
            _indexError = string.Empty;
            _byId = new Dictionary<DiceId, DiceDefinition>();

            if (_definitions == null)
            {
                _indexError = $"{name}: definitions collection is missing.";
                return false;
            }

            for (int i = 0; i < _definitions.Count; i++)
            {
                DiceDefinition definition = _definitions[i];
                if (definition == null)
                {
                    _indexError = $"{name}: definition {i + 1} is missing.";
                    return false;
                }

                if (!definition.TryValidate(out string definitionError))
                {
                    _indexError = definitionError;
                    return false;
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    _indexError = $"{name}: duplicate dice ID '{definition.Id}'.";
                    return false;
                }

                _byId.Add(definition.Id, definition);
            }

            return true;
        }
    }
}
