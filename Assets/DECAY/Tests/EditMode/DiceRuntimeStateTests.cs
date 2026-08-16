
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Decay.Tests
{
    public sealed class DiceRuntimeStateTests
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
        public void PlayerRuntimeState_KeepsDefinitionOwnedAndBattleIdentitySeparate()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition("dice.balanced_d6"));
            DiceRuntimeState state = DiceRuntimeState.CreatePlayerDice(
                new DiceInstanceId(101),
                new OwnedDiceId(12),
                definition);

            Assert.That(state.DefinitionId, Is.EqualTo(new DiceId("dice.balanced_d6")));
            Assert.That(state.SourceOwnedDiceId, Is.EqualTo(new OwnedDiceId(12)));
            Assert.That(state.InstanceId, Is.EqualTo(new DiceInstanceId(101)));
            Assert.That(state.Owner, Is.EqualTo(Side.Player));
        }

        [Test]
        public void EnemyRuntimeState_HasNoGlobalOwnedDiceSource()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);

            Assert.That(state.HasSourceOwnedDice, Is.False);
            Assert.That(state.SourceOwnedDiceId.IsValid, Is.False);
            Assert.That(state.Owner, Is.EqualTo(Side.Enemy));
        }

        [Test]
        public void RuntimeState_ClonesMutableFaceValuesFromDefinition()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);

            Assert.That(state.TryGetFace(2, out DiceFaceRuntimeState runtimeFace), Is.True);
            runtimeFace.SetRollValue(6);
            runtimeFace.SetScoreValue(20);

            Assert.That(runtimeFace.RollValue, Is.EqualTo(6));
            Assert.That(runtimeFace.ScoreValue, Is.EqualTo(20));
            Assert.That(definition.Faces[1].RollValue, Is.EqualTo(2));
            Assert.That(definition.Faces[1].BaseScoreValue, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void RuntimeRollValueMutation_RejectsValuesOutsideOneToSix(int invalidRollValue)
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);
            Assert.That(state.TryGetFace(2, out DiceFaceRuntimeState runtimeFace), Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => runtimeFace.SetRollValue(invalidRollValue));
            Assert.That(runtimeFace.RollValue, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void RuntimeSeed_RejectsRollValuesOutsideOneToSix(int invalidRollValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceFaceSeed(1, invalidRollValue, 1));
        }

        [Test]
        public void AuthoritativeFaceSelection_UsesFaceIndexAndExposesSeparateValues()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                generalScoreValue: 5,
                rollValues: new[] { 6, 1, 4 },
                faceScoreValues: new[] { 2, 10, 7 }));
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);

            state.SetCurrentFace(2);

            Assert.That(state.CurrentFaceIndex, Is.EqualTo(2));
            Assert.That(state.ActiveRollValue, Is.EqualTo(1));
            Assert.That(state.ActiveFaceScoreValue, Is.EqualTo(10));
            Assert.That(state.GeneralScoreValue, Is.EqualTo(5));
            Assert.That(state.ActiveScoreContribution, Is.EqualTo(15));
        }

        [Test]
        public void CurrentFace_IsUnavailableUntilAuthoritativeRollIsApplied()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);

            Assert.That(state.HasCurrentFace, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = state.CurrentFace);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.SetCurrentFace(99));
        }

        [Test]
        public void DecayedDice_TracksBattleLocalDecayWithoutOwningBoardOrInventoryLocation()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);
            state.SetCurrentFace(6);

            state.MarkDecayedForCurrentGame();

            Assert.That(state.IsDecayedForCurrentGame, Is.True);
            Assert.That(state.HasCurrentFace, Is.False);
        }

        [Test]
        public void GameReset_RestoresDefinitionValuesWithoutChangingIdentity()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                "dice.resettable_d6",
                generalScoreValue: 2));
            DiceRuntimeState state = DiceRuntimeState.CreatePlayerDice(
                new DiceInstanceId(44),
                new OwnedDiceId(9),
                definition);
            state.SetGeneralScoreValue(99);
            state.TryGetFace(1, out DiceFaceRuntimeState face);
            face.SetRollValue(6);
            state.MarkDecayedForCurrentGame();

            var globalInventorySeed = new DiceRuntimeSeed(
                definition.Id,
                7,
                new[]
                {
                    new DiceFaceSeed(1, 3, 30),
                    new DiceFaceSeed(2, 2, 2),
                    new DiceFaceSeed(3, 3, 3),
                    new DiceFaceSeed(4, 4, 4),
                    new DiceFaceSeed(5, 5, 5),
                    new DiceFaceSeed(6, 6, 6)
                });

            state.ResetFromSeed(globalInventorySeed);

            Assert.That(state.InstanceId, Is.EqualTo(new DiceInstanceId(44)));
            Assert.That(state.SourceOwnedDiceId, Is.EqualTo(new OwnedDiceId(9)));
            Assert.That(state.GeneralScoreValue, Is.EqualTo(7));
            Assert.That(state.Faces[0].RollValue, Is.EqualTo(3));
            Assert.That(state.Faces[0].ScoreValue, Is.EqualTo(30));
            Assert.That(definition.BaseGeneralScoreValue, Is.EqualTo(2));
            Assert.That(state.IsDecayedForCurrentGame, Is.False);
        }

        [Test]
        public void RuntimeState_CopiesTagsForBattleLocalMutation()
        {
            var starter = new DiceTagId("tag.starter");
            var temporary = new DiceTagId("tag.temporary");
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(tags: new[] { starter }));
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);

            state.AddTag(temporary);
            state.RemoveTag(starter);

            Assert.That(state.HasTag(temporary), Is.True);
            Assert.That(state.HasTag(starter), Is.False);
            Assert.That(definition.Tags, Does.Contain(starter));
        }

        [Test]
        public void EffectRuntimeState_AllowsMultipleOccurrencesOfSameEffectDefinition()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);
            var effectId = new EffectId("effect.repeated_score_gain");
            var first = new TestEffectRuntimeState(new EffectInstanceId(1), effectId);
            var second = new TestEffectRuntimeState(new EffectInstanceId(2), effectId);

            state.RegisterEffectRuntimeState(first);
            state.RegisterEffectRuntimeState(second);

            Assert.That(state.EffectRuntimeStateCount, Is.EqualTo(2));
            Assert.That(state.TryGetEffectRuntimeState(first.InstanceId, out IEffectRuntimeState foundFirst), Is.True);
            Assert.That(state.TryGetEffectRuntimeState(second.InstanceId, out IEffectRuntimeState foundSecond), Is.True);
            Assert.That(foundFirst.EffectId, Is.EqualTo(effectId));
            Assert.That(foundSecond.EffectId, Is.EqualTo(effectId));
        }

        [Test]
        public void EffectRuntimeState_RejectsDuplicateOccurrenceIdentity()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition());
            DiceRuntimeState state = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);
            var effectId = new EffectId("effect.repeated_score_gain");
            var first = new TestEffectRuntimeState(new EffectInstanceId(1), effectId);
            var duplicateInstance = new TestEffectRuntimeState(new EffectInstanceId(1), effectId);

            state.RegisterEffectRuntimeState(first);

            Assert.Throws<InvalidOperationException>(() => state.RegisterEffectRuntimeState(duplicateInstance));
        }

        private T Track<T>(T created) where T : Object
        {
            _createdObjects.Add(created);
            return created;
        }

        private sealed class TestEffectRuntimeState : IEffectRuntimeState
        {
            public TestEffectRuntimeState(EffectInstanceId instanceId, EffectId effectId)
            {
                InstanceId = instanceId;
                EffectId = effectId;
            }

            public EffectInstanceId InstanceId { get; }
            public EffectId EffectId { get; }
        }
    }
}
