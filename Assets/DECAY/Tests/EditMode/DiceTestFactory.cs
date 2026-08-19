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


        public static BattleState CreateBattleState()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();
            try
            {
                return new BattleState(config);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        public static DiceRuntimeState CreatePlayerRuntimeDice(
            long instanceId,
            long ownedDiceId,
            string definitionId = "dice.neutral_d6")
        {
            DiceDefinition definition = CreateDefinition(definitionId);
            DiceRuntimeState state = DiceRuntimeState.CreatePlayerDice(
                new DiceInstanceId(instanceId),
                new OwnedDiceId(ownedDiceId),
                definition);
            Object.DestroyImmediate(definition);
            return state;
        }

        public static DiceRuntimeState CreateEnemyRuntimeDice(
            long instanceId,
            string definitionId = "dice.enemy_neutral_d6")
        {
            DiceDefinition definition = CreateDefinition(definitionId);
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(instanceId), definition);
            Object.DestroyImmediate(definition);
            return state;
        }
    }
}
