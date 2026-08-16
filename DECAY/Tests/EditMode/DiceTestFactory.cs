
using System.Collections.Generic;
using UnityEngine;

namespace Decay.Tests
{
    internal static class DiceTestFactory
    {
        public static DiceDefinition CreateDefinition(
            string id = "dice.neutral_d6",
            int generalScoreValue = 0,
            IReadOnlyList<int> rollValues = null,
            IReadOnlyList<int> faceScoreValues = null,
            IReadOnlyList<DiceTagId> tags = null,
            IReadOnlyList<EffectDefinition> effects = null)
        {
            rollValues = rollValues ?? new[] { 1, 2, 3, 4, 5, 6 };
            faceScoreValues = faceScoreValues ?? rollValues;

            var faces = new List<DiceFaceDefinition>();
            for (int i = 0; i < rollValues.Count; i++)
            {
                faces.Add(new DiceFaceDefinition(i + 1, rollValues[i], faceScoreValues[i]));
            }

            DiceDefinition definition = ScriptableObject.CreateInstance<DiceDefinition>();
            definition.name = $"def_DICE_{id}";
            definition.ConfigureForTests(
                new DiceId(id),
                id,
                generalScoreValue,
                faces,
                tags,
                effects);
            return definition;
        }
    }
}
