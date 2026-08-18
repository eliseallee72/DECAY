
using System;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class ContentIdentityTests
    {
        [Test]
        public void DiceId_TrimsAndUsesOrdinalIdentity()
        {
            var first = new DiceId("  dice.neutral_d6 ");
            var same = new DiceId("dice.neutral_d6");
            var different = new DiceId("dice.neutral_d7");

            Assert.That(first.Value, Is.EqualTo("dice.neutral_d6"));
            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(different));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ContentIds_RejectEmptyValues(string value)
        {
            Assert.Throws<ArgumentException>(() => new DiceId(value));
            Assert.Throws<ArgumentException>(() => new EffectId(value));
            Assert.Throws<ArgumentException>(() => new DiceTagId(value));
        }

        [TestCase("neutral-d6")]
        [TestCase("Dice.neutral_d6")]
        [TestCase("dice.Neutral_d6")]
        [TestCase("dice.neutral-d6")]
        [TestCase("dice")]
        [TestCase("dice.foo.bar")]
        [TestCase("effect.neutral_d6")]
        public void DiceId_RejectsValuesOutsideStableDiceCategoryFormat(string value)
        {
            Assert.Throws<ArgumentException>(() => new DiceId(value));
        }

        [Test]
        public void EffectAndTagIds_RequireTheirOwnCategories()
        {
            Assert.That(new EffectId("effect.score_bonus").IsValid, Is.True);
            Assert.That(new DiceTagId("tag.starter").IsValid, Is.True);
            Assert.Throws<ArgumentException>(() => new EffectId("dice.score_bonus"));
            Assert.Throws<ArgumentException>(() => new DiceTagId("effect.starter"));
        }

        [Test]
        public void OwnedDiceId_IsSeparateFromBattleInstanceIdentity()
        {
            var owned = new OwnedDiceId(7);
            var battle = new DiceInstanceId(7);

            Assert.That(owned.Value, Is.EqualTo(battle.Value));
            Assert.That(owned.GetType(), Is.Not.EqualTo(battle.GetType()));
        }

        [Test]
        public void OwnedDiceId_RequiresPositiveValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OwnedDiceId(0));
        }

        [Test]
        public void EffectInstanceId_IsSeparateFromEffectDefinitionIdentity()
        {
            var definitionId = new EffectId("effect.score_bonus");
            var firstOccurrence = new EffectInstanceId(1);
            var secondOccurrence = new EffectInstanceId(2);

            Assert.That(definitionId.IsValid, Is.True);
            Assert.That(firstOccurrence, Is.Not.EqualTo(secondOccurrence));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EffectInstanceId(0));
        }
    }
}
