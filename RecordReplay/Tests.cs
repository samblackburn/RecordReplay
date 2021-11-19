﻿using System;
using System.Text.Json;
using Moq;
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
            var rr = new RecordReplay<IFoo>(kvs, getNewObject, x => JsonSerializer.Serialize(x));
            rr.Setup<string, int>(f => f.Append(It.IsAny<string>()), (f, s) => f.Append(s));
            var obj = rr.Object;
            obj.Append("HELLO");
            var output = obj.Append("GOODBYE");
            Assert.AreEqual(output, ("HELLO" + "GOODBYE").Length);
        }
    }
}
