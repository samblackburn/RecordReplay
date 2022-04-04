using System;
using NUnit.Framework;

namespace RecordReplay
{
    public class Tests
    {
        [Test]
        public void ReplayRunsDoNotCallImplementation()
        {
            var kvs = new InMemoryKeyValueStore();
            ConcatenateTwoWords(kvs, () => new SlowFoo());
            ConcatenateTwoWords(kvs, () => throw new AssertionException("Should not need implementation for replay run"));
        }

        private static void ConcatenateTwoWords(IKeyValueStore kvs, Func<IFoo> getNewObject)
        {
            var rr = RecordReplay<IFoo>.Create(kvs, getNewObject);
            rr.Append("HELLO");
            var output = rr.Append("GOODBYE");
            Assert.AreEqual(output, ("HELLO" + "GOODBYE").Length);
        }
    }
}
