
using System;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class BattleHistoryTests
    {
        [Test]
        public void Record_AssignsMonotonicSequenceNumbersInResolutionOrder()
        {
            var history = new BattleHistory();
            var first = history.Record(new TestBattleFact("first"));
            var second = history.Record(new TestBattleFact("second"));

            Assert.That(first.SequenceNumber, Is.EqualTo(1));
            Assert.That(second.SequenceNumber, Is.EqualTo(2));
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history.Facts[0], Is.SameAs(first));
            Assert.That(history.Facts[1], Is.SameAs(second));
        }

        [Test]
        public void Record_RejectsRecordingSameFactTwice()
        {
            var history = new BattleHistory();
            var fact = history.Record(new TestBattleFact("once"));

            Assert.Throws<InvalidOperationException>(() => history.Record(fact));
        }

        private sealed class TestBattleFact : BattleFact
        {
            public TestBattleFact(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }
    }
}
