using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    [CreateAssetMenu(fileName = "def_DICE_New", menuName = "DECAY/Dice/Dice Definition")]
    public sealed class DiceDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private DiceId _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Rules")]
        [SerializeField] private int _baseGeneralScoreValue;
        [SerializeField] private List<DiceFaceDefinition> _faces = new List<DiceFaceDefinition>();
        [SerializeField] private List<DiceTagId> _tags = new List<DiceTagId>();
        [SerializeField] private List<EffectDefinition> _effects = new List<EffectDefinition>();

        [Header("Presentation References")]
        [SerializeField] private Sprite _inventorySprite;
        [SerializeField] private Sprite _boardSprite;
        [SerializeField] private GameObject _viewPrefab;

        public DiceId Id => _id;
        public string DisplayName => _displayName ?? string.Empty;
        public string Description => _description ?? string.Empty;
        public int BaseGeneralScoreValue => _baseGeneralScoreValue;
        public IReadOnlyList<DiceFaceDefinition> Faces => _faces;
        public IReadOnlyList<DiceTagId> Tags => _tags;
        public IReadOnlyList<EffectDefinition> Effects => _effects;
        public Sprite InventorySprite => _inventorySprite;
        public Sprite BoardSprite => _boardSprite;
        public GameObject ViewPrefab => _viewPrefab;
        public int FaceCount => _faces == null ? 0 : _faces.Count;

        public bool TryGetFace(int faceIndex, out DiceFaceDefinition face)
        {
            face = null;
            if (_faces == null || faceIndex < 1 || faceIndex > _faces.Count)
            {
                return false;
            }

            DiceFaceDefinition candidate = _faces[faceIndex - 1];
            if (candidate == null || candidate.FaceIndex != faceIndex)
            {
                return false;
            }

            face = candidate;
            return true;
        }

        public bool TryValidate(out string error)
        {
            if (!_id.IsValid)
            {
                error = $"{name}: dice ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                error = $"{name}: display name is required.";
                return false;
            }

            if (_faces == null || _faces.Count == 0)
            {
                error = $"{name}: at least one face is required.";
                return false;
            }

            for (int i = 0; i < _faces.Count; i++)
            {
                DiceFaceDefinition face = _faces[i];
                if (face == null)
                {
                    error = $"{name}: face {i + 1} is missing.";
                    return false;
                }

                if (face.FaceIndex != i + 1)
                {
                    error = $"{name}: faces must be stored in explicit 1-based order.";
                    return false;
                }

                if (!face.TryValidate(out error))
                {
                    error = $"{name}: {error}";
                    return false;
                }
            }

            if (!ValidateTags(out error) || !ValidateEffects(out error))
            {
                error = $"{name}: {error}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void ConfigureForTests(
            DiceId id,
            string displayName,
            int baseGeneralScoreValue,
            IEnumerable<DiceFaceDefinition> faces,
            IEnumerable<DiceTagId> tags = null,
            IEnumerable<EffectDefinition> effects = null)
        {
            _id = id;
            _displayName = displayName;
            _baseGeneralScoreValue = baseGeneralScoreValue;
            _faces = faces == null ? new List<DiceFaceDefinition>() : new List<DiceFaceDefinition>(faces);
            _tags = tags == null ? new List<DiceTagId>() : new List<DiceTagId>(tags);
            _effects = effects == null ? new List<EffectDefinition>() : new List<EffectDefinition>(effects);
        }

        internal void ConfigurePresentationForTests(
            Sprite inventorySprite,
            Sprite boardSprite,
            GameObject viewPrefab)
        {
            _inventorySprite = inventorySprite;
            _boardSprite = boardSprite;
            _viewPrefab = viewPrefab;
        }

        private bool ValidateTags(out string error)
        {
            if (_tags == null)
            {
                error = "tags collection is missing.";
                return false;
            }

            var uniqueTags = new HashSet<DiceTagId>();
            for (int i = 0; i < _tags.Count; i++)
            {
                if (!_tags[i].IsValid || !uniqueTags.Add(_tags[i]))
                {
                    error = "tag IDs must be valid and unique.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private bool ValidateEffects(out string error)
        {
            if (_effects == null)
            {
                error = "effects collection is missing.";
                return false;
            }

            var uniqueEffects = new HashSet<EffectId>();
            for (int i = 0; i < _effects.Count; i++)
            {
                EffectDefinition effect = _effects[i];
                if (effect == null)
                {
                    error = $"effect {i + 1} is missing.";
                    return false;
                }

                if (!effect.TryValidate(out string effectError))
                {
                    error = $"effect {i + 1} is invalid: {effectError}";
                    return false;
                }

                if (!uniqueEffects.Add(effect.Id))
                {
                    error = "effect IDs must be unique within the dice definition.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
