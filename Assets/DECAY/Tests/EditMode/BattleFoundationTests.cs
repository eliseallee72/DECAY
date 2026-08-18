using System;
using NUnit.Framework;
using UnityEngine;

namespace Decay.Tests
{
    public sealed class BattleFoundationTests
    {
        [Test]
        public void BattleConfig_DefaultsMatchAuthoritativeRules()
        {
            BattleConfig config = ScriptableObject.CreateInstance<BattleConfig>();

            Assert.That(config.GamesPerBattle, Is.EqualTo(2));
            Assert.That(config.RoundsPerGame, Is.EqualTo(4));
            Assert.That(config.BattleInventoryCapacity, Is.EqualTo(10));
            Assert.That(config.TryValidate(out string error), Is.True, error);

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void BattleRules_DefineSixSlotsPerSideAndRollValueBounds()
        {
            Assert.That(BattleRules.SlotsPerSide, Is.EqualTo(6));
            Assert.That(BattleRules.FirstSlotNumber, Is.EqualTo(1));
            Assert.That(BattleRules.LastSlotNumber, Is.EqualTo(6));
            Assert.That(BattleRules.MinimumRollValue, Is.EqualTo(1));
            Assert.That(BattleRules.MaximumRollValue, Is.EqualTo(6));
        }

        [TestCase(Side.Enemy, 1, "1E")]
        [TestCase(Side.Enemy, 6, "6E")]
        [TestCase(Side.Player, 1, "1P")]
        [TestCase(Side.Player, 6, "6P")]
        public void SlotId_UsesDocumentedLabels(Side side, int number, string expected)
        {
            SlotId slotId = new SlotId(side, number);

            Assert.That(slotId.Number, Is.EqualTo(number));
            Assert.That(slotId.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void SlotId_OpposingPreservesNumberAndChangesSide()
        {
            SlotId enemy = new SlotId(Side.Enemy, 3);

            Assert.That(enemy.Opposing, Is.EqualTo(new SlotId(Side.Player, 3)));
            Assert.That(enemy.Opposing.Opposing, Is.EqualTo(enemy));
        }

        [Test]
        public void SlotPairId_GroupsOpposingSlotsAsOneNumberedPair()
        {
            var pair = new SlotPairId(4);

            Assert.That(pair.Number, Is.EqualTo(4));
            Assert.That(pair.EnemySlot, Is.EqualTo(new SlotId(Side.Enemy, 4)));
            Assert.That(pair.PlayerSlot, Is.EqualTo(new SlotId(Side.Player, 4)));
            Assert.That(pair.EnemySlot.Opposing, Is.EqualTo(pair.PlayerSlot));
        }

        [Test]
        public void SlotIndexConverter_IsTheSharedOneBasedToStorageConversion()
        {
            SlotId slot = new SlotId(Side.Player, 6);

            Assert.That(SlotIndexConverter.ToStorageIndex(slot), Is.EqualTo(5));
            Assert.That(SlotIndexConverter.FromStorageIndex(Side.Player, 5), Is.EqualTo(slot));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void SlotId_RejectsOutOfRangeNumber(int number)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SlotId(Side.Player, number));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SlotPairId(number));
        }

        [Test]
        public void SlotId_RejectsUnknownSide()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SlotId((Side)99, 1));
        }

        [Test]
        public void DefaultSlotId_IsExplicitlyInvalid()
        {
            SlotId slot = default;

            Assert.That(slot.IsValid, Is.False);
            Assert.That(slot.ToString(), Is.EqualTo("None"));
            Assert.Throws<InvalidOperationException>(() => _ = slot.Opposing);
        }

        [Test]
        public void SlotId_EqualityIncludesSideAndNumber()
        {
            Assert.That(new SlotId(Side.Player, 2), Is.EqualTo(new SlotId(Side.Player, 2)));
            Assert.That(new SlotId(Side.Player, 2), Is.Not.EqualTo(new SlotId(Side.Enemy, 2)));
            Assert.That(new SlotId(Side.Player, 2), Is.Not.EqualTo(new SlotId(Side.Player, 3)));
        }

        [Test]
        public void DiceInstanceId_RequiresPositiveValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceInstanceId(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceInstanceId(-1));
        }

        [Test]
        public void DiceInstanceId_UsesValueIdentity()
        {
            DiceInstanceId first = new DiceInstanceId(12);
            DiceInstanceId same = new DiceInstanceId(12);
            DiceInstanceId different = new DiceInstanceId(13);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(different));
            Assert.That(first.IsValid, Is.True);
        }

        [Test]
        public void PhaseTransitionValidator_AllowsCoreLoopWithoutDependingOnEnumArithmetic()
        {
            var validator = new BattlePhaseTransitionValidator();

            Assert.That(validator.IsTransitionAllowed(BattlePhase.Setup, BattlePhase.Rolling), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.Rolling, BattlePhase.EnemyReposition), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.EnemyReposition, BattlePhase.PlayerReposition), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.PlayerReposition, BattlePhase.DecayProcess), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.DecayProcess, BattlePhase.ScoreProcess), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.ScoreProcess, BattlePhase.RoundEnd), Is.True);
        }

        [Test]
        public void PhaseTransitionValidator_ExpressesRoundAndGameBranchesExplicitly()
        {
            var validator = new BattlePhaseTransitionValidator();

            Assert.That(validator.IsTransitionAllowed(BattlePhase.RoundEnd, BattlePhase.Setup), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.RoundEnd, BattlePhase.GameEnd), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.GameEnd, BattlePhase.Setup), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.GameEnd, BattlePhase.BattleEnd), Is.True);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.BattleEnd, BattlePhase.Setup), Is.False);
        }

        [Test]
        public void PhaseTransitionValidator_RejectsSkippingProcessSteps()
        {
            var validator = new BattlePhaseTransitionValidator();

            Assert.That(validator.IsTransitionAllowed(BattlePhase.Setup, BattlePhase.EnemyReposition), Is.False);
            Assert.That(validator.IsTransitionAllowed(BattlePhase.Setup, BattlePhase.DecayProcess), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                validator.RequireAllowedTransition(BattlePhase.Setup, BattlePhase.DecayProcess));
        }
    }
}
