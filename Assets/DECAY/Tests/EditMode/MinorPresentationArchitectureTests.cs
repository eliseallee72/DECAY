using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Decay.Tests
{
    public sealed class MinorPresentationArchitectureTests
    {
        [Test]
        public void CodedMotionSettings_DefaultsToUnconfigured()
        {
            var settings = new BattlePresentationSettings.CodedMotionSettings();

            Assert.That(settings.IsConfigured, Is.False,
                "Destination movement requires editor-authored duration and easing before it may animate.");
        }

        [Test]
        public void RollStartOffsetRange_DefaultsToNoDelay()
        {
            var settings = new BattlePresentationSettings();

            Assert.That(settings.RollStartOffsetRange, Is.EqualTo(Vector2.zero),
                "Minor Roll staggering must remain opt-in editor tuning rather than a hard-coded visual default.");
        }

        [Test]
        public void BattleDiceMovementPresenter_DoesNotHoldGameplayAuthorityObjects()
        {
            Type presenterType = typeof(BattleDiceMovementPresenter);
            Type[] forbiddenAuthorityTypes =
            {
                typeof(BattleController),
                typeof(BoardState),
                typeof(BattleState),
                typeof(MoveDiceController)
            };

            FieldInfo[] fields = presenterType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (Type forbiddenType in forbiddenAuthorityTypes)
            {
                Assert.That(fields.Any(field => forbiddenType.IsAssignableFrom(field.FieldType)), Is.False,
                    $"Presentation movement must not own or directly mutate {forbiddenType.Name}.");
            }
        }

        [Test]
        public void PointerPresentationTarget_ContainsNoGameplayResultContract()
        {
            MethodInfo[] methods = typeof(IPointerPresentationTarget).GetMethods();

            Assert.That(methods.Any(method => method.ReturnType != typeof(void) && method.Name != "get_PointerPresentationEnabled"), Is.False,
                "Pointer presentation feedback must not return an interaction/gameplay result.");
        }

        [Test]
        public void HourglassView_DoesNotExposeDecayPresentationContract()
        {
            MethodInfo[] methods = typeof(HourglassView).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(methods.Any(method => method.Name.IndexOf("DecayPresentation", StringComparison.Ordinal) >= 0), Is.False,
                "Hourglass presentation must not own dice/slot Decay animation hooks.");
        }

        [Test]
        public void HourglassView_DoesNotOwnBattleAuthorityObjects()
        {
            FieldInfo[] fields = typeof(HourglassView).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(fields.Any(field => typeof(BattleController).IsAssignableFrom(field.FieldType)), Is.False);
            Assert.That(fields.Any(field => typeof(BattleState).IsAssignableFrom(field.FieldType)), Is.False,
                "Hourglass interaction availability may reflect authoritative phase but must not own battle authority.");
        }
    }
}
