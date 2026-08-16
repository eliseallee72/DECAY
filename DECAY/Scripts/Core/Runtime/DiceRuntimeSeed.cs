
using System;
using System.Collections.Generic;

namespace Decay
{
    // Runtime construction data only. Persistence must serialize stable IDs and
    // save data rather than Unity EffectDefinition object references.
    public sealed class DiceRuntimeSeed
    {
        private readonly List<DiceFaceSeed> _faces;
        private readonly List<DiceTagId> _tags;
        private readonly List<EffectDefinition> _effects;

        internal DiceRuntimeSeed(
            DiceId definitionId,
            int generalScoreValue,
            IEnumerable<DiceFaceSeed> faces,
            IEnumerable<DiceTagId> tags = null,
            IEnumerable<EffectDefinition> effects = null)
        {
            if (!definitionId.IsValid)
            {
                throw new ArgumentException("A valid dice definition ID is required.", nameof(definitionId));
            }

            DefinitionId = definitionId;
            GeneralScoreValue = generalScoreValue;
            _faces = faces == null ? new List<DiceFaceSeed>() : new List<DiceFaceSeed>(faces);
            _tags = tags == null ? new List<DiceTagId>() : new List<DiceTagId>(tags);
            _effects = effects == null ? new List<EffectDefinition>() : new List<EffectDefinition>(effects);

            if (!TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(faces));
            }
        }

        public DiceId DefinitionId { get; }
        public int GeneralScoreValue { get; }
        public IReadOnlyList<DiceFaceSeed> Faces => _faces;
        public IReadOnlyList<DiceTagId> Tags => _tags;
        public IReadOnlyList<EffectDefinition> Effects => _effects;

        public static DiceRuntimeSeed FromDefinition(DiceDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!definition.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(definition));
            }

            var faces = new List<DiceFaceSeed>(definition.Faces.Count);
            for (int i = 0; i < definition.Faces.Count; i++)
            {
                DiceFaceDefinition face = definition.Faces[i];
                faces.Add(new DiceFaceSeed(
                    face.FaceIndex,
                    face.RollValue,
                    face.BaseScoreValue,
                    face.Effects));
            }

            return new DiceRuntimeSeed(
                definition.Id,
                definition.BaseGeneralScoreValue,
                faces,
                definition.Tags,
                definition.Effects);
        }

        public bool TryValidate(out string error)
        {
            if (!DefinitionId.IsValid)
            {
                error = "Runtime seed requires a valid definition ID.";
                return false;
            }

            if (_faces == null || _faces.Count == 0)
            {
                error = "Runtime seed requires at least one face.";
                return false;
            }

            for (int i = 0; i < _faces.Count; i++)
            {
                DiceFaceSeed face = _faces[i];
                if (face == null || face.FaceIndex != i + 1)
                {
                    error = "Runtime seed faces must be stored in explicit 1-based order.";
                    return false;
                }

                if (!RollValueRules.IsValid(face.RollValue))
                {
                    error = $"Runtime seed face {i + 1} roll value must be between {BattleRules.MinimumRollValue} and {BattleRules.MaximumRollValue}.";
                    return false;
                }

                var faceEffectIds = new HashSet<EffectId>();
                for (int effectIndex = 0; effectIndex < face.Effects.Count; effectIndex++)
                {
                    EffectDefinition effect = face.Effects[effectIndex];
                    if (effect == null)
                    {
                        error = $"Runtime seed face {i + 1} effect {effectIndex + 1} is missing.";
                        return false;
                    }

                    if (!effect.TryValidate(out string effectError))
                    {
                        error = $"Runtime seed face {i + 1} effect {effectIndex + 1} is invalid: {effectError}";
                        return false;
                    }

                    if (!faceEffectIds.Add(effect.Id))
                    {
                        error = $"Runtime seed face {i + 1} effect IDs must be unique within that face.";
                        return false;
                    }
                }
            }

            var uniqueTags = new HashSet<DiceTagId>();
            for (int i = 0; i < _tags.Count; i++)
            {
                if (!_tags[i].IsValid || !uniqueTags.Add(_tags[i]))
                {
                    error = "Runtime seed tag IDs must be valid and unique.";
                    return false;
                }
            }

            var uniqueEffects = new HashSet<EffectId>();
            for (int i = 0; i < _effects.Count; i++)
            {
                EffectDefinition effect = _effects[i];
                if (effect == null)
                {
                    error = $"Runtime seed effect {i + 1} is missing.";
                    return false;
                }

                if (!effect.TryValidate(out string effectError))
                {
                    error = $"Runtime seed effect {i + 1} is invalid: {effectError}";
                    return false;
                }

                if (!uniqueEffects.Add(effect.Id))
                {
                    error = "Runtime seed effect IDs must be unique within the dice definition.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
