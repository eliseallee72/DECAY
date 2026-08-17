using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Decay.Tests
{
    public sealed class DiceCatalogTests
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
        public void Catalog_ResolvesStableDiceId()
        {
            DiceDefinition neutral = Track(DiceTestFactory.CreateDefinition("dice.neutral_d6"));
            DiceDefinition cracked = Track(DiceTestFactory.CreateDefinition("dice.cracked_d6"));
            DiceCatalog catalog = Track(ScriptableObject.CreateInstance<DiceCatalog>());
            catalog.ConfigureForTests(new[] { neutral, cracked });

            Assert.That(catalog.TryValidate(out string error), Is.True, error);
            Assert.That(catalog.TryGet(new DiceId("dice.cracked_d6"), out DiceDefinition result), Is.True);
            Assert.That(result, Is.SameAs(cracked));
        }

        [Test]
        public void Catalog_RejectsDuplicateDiceIds()
        {
            DiceDefinition first = Track(DiceTestFactory.CreateDefinition("dice.duplicate"));
            DiceDefinition second = Track(DiceTestFactory.CreateDefinition("dice.duplicate"));
            DiceCatalog catalog = Track(ScriptableObject.CreateInstance<DiceCatalog>());
            catalog.ConfigureForTests(new[] { first, second });

            Assert.That(catalog.TryValidate(out string error), Is.False);
            StringAssert.Contains("duplicate dice ID", error);
            Assert.Throws<InvalidOperationException>(() => catalog.GetRequired(new DiceId("dice.duplicate")));
        }

        [Test]
        public void Catalog_GetRequiredReportsMissingId()
        {
            DiceCatalog catalog = Track(ScriptableObject.CreateInstance<DiceCatalog>());
            catalog.ConfigureForTests(Array.Empty<DiceDefinition>());

            Assert.Throws<KeyNotFoundException>(() => catalog.GetRequired(new DiceId("dice.missing")));
        }

        private T Track<T>(T created) where T : Object
        {
            _createdObjects.Add(created);
            return created;
        }
    }
}
