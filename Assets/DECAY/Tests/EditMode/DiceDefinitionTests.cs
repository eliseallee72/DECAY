
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Decay.Tests
{
    public sealed class DiceDefinitionTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Definition_DefaultD6KeepsRollAndScoreValuesDistinct()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                generalScoreValue: 3,
                rollValues: new[] { 1, 2, 3, 4, 5, 6 },
                faceScoreValues: new[] { 10, 20, 30, 40, 50, 60 }));

            Assert.That(definition.TryValidate(out string error), Is.True, error);
            Assert.That(definition.BaseGeneralScoreValue, Is.EqualTo(3));
            Assert.That(definition.Faces[1].RollValue, Is.EqualTo(2));
            Assert.That(definition.Faces[1].BaseScoreValue, Is.EqualTo(20));
        }

        [Test]
        public void Definition_AllowsRepeatedAndNonSequentialRollValuesWithinOneToSix()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                rollValues: new[] { 1, 1, 2, 4, 6, 6 }));

            Assert.That(definition.TryValidate(out string error), Is.True, error);
            Assert.That(definition.FaceCount, Is.EqualTo(6));
            Assert.That(definition.Faces[0].RollValue, Is.EqualTo(1));
            Assert.That(definition.Faces[1].RollValue, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void Definition_RejectsRollValuesOutsideOneToSix(int invalidRollValue)
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                rollValues: new[] { invalidRollValue }));

            Assert.That(definition.TryValidate(out string error), Is.False);
            StringAssert.Contains("roll value", error);
        }

        [Test]
        public void Definition_RequiresExplicitOrderedFaceIndices()
        {
            DiceDefinition definition = Track(ScriptableObject.CreateInstance<DiceDefinition>());
            definition.ConfigureForTests(
                new DiceId("dice.bad_order"),
                "Bad Order",
                0,
                new[]
                {
                    new DiceFaceDefinition(2, 1, 1),
                    new DiceFaceDefinition(1, 2, 2)
                });

            Assert.That(definition.TryValidate(out string error), Is.False);
            StringAssert.Contains("1-based order", error);
        }

        [Test]
        public void Definition_RequiresUniqueTypedTags()
        {
            var tag = new DiceTagId("tag.starter");
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(tags: new[] { tag, tag }));

            Assert.That(definition.TryValidate(out string error), Is.False);
            StringAssert.Contains("tag IDs", error);
        }

        [Test]
        public void Definition_RecursivelyValidatesEffectDefinitions()
        {
            TestEffectDefinition invalidEffect = Track(ScriptableObject.CreateInstance<TestEffectDefinition>());
            invalidEffect.ConfigureForTests(default, "Invalid Effect");
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(effects: new EffectDefinition[] { invalidEffect }));

            Assert.That(definition.TryValidate(out string error), Is.False);
            StringAssert.Contains("effect 1 is invalid", error);
        }

        [Test]
        public void TryGetFace_UsesFaceIndexNotRollValue()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                rollValues: new[] { 6, 6, 1 }));

            Assert.That(definition.TryGetFace(2, out DiceFaceDefinition face), Is.True);
            Assert.That(face.FaceIndex, Is.EqualTo(2));
            Assert.That(face.RollValue, Is.EqualTo(6));
            Assert.That(definition.TryGetFace(6, out _), Is.False);
        }

        private T Track<T>(T created) where T : Object
        {
            _createdObjects.Add(created);
            return created;
        }

    }
}
