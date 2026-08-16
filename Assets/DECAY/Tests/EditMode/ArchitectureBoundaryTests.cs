using System.Reflection;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class ArchitectureBoundaryTests
    {
        [Test]
        public void BattlePhaseController_Handle_IsInternalSoGameplayCannotBypassBattleController()
        {
            MethodInfo publicMethod = typeof(BattlePhaseController).GetMethod(
                "Handle",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo internalMethod = typeof(BattlePhaseController).GetMethod(
                "Handle",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(publicMethod, Is.Null);
            Assert.That(internalMethod, Is.Not.Null);
            Assert.That(internalMethod.IsAssembly, Is.True);
        }

        [Test]
        public void RollExecutor_ExecuteRoll_IsInternalSoGameplayCannotBypassBattleFlow()
        {
            MethodInfo publicMethod = typeof(RollExecutor).GetMethod(
                "ExecuteRoll",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo internalMethod = typeof(RollExecutor).GetMethod(
                "ExecuteRoll",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(publicMethod, Is.Null);
            Assert.That(internalMethod, Is.Not.Null);
            Assert.That(internalMethod.IsAssembly, Is.True);
        }
    }
}
