using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class MinorPresentationArchitectureTests
    {
        [Test]
        public void ProceduralTransformPresentationBinding_DefaultsToUnconfigured()
        {
            var binding = new ProceduralTransformPresentationBinding();

            Assert.That(binding.IsConfigured, Is.False,
                "Minor animation infrastructure must not ship a hard-coded motion fallback.");
        }

        [Test]
        public void CodedMotionSettings_DefaultsToUnconfigured()
        {
            var settings = new BattlePresentationSettings.CodedMotionSettings();

            Assert.That(settings.IsConfigured, Is.False,
                "Destination movement requires editor-authored duration and easing before it may animate.");
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
    }
}
