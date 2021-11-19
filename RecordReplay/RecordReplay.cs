using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Moq;

namespace RecordReplay
{
    public class RecordReplay<TFoo>
        where TFoo : class
    {
        private readonly IKeyValueStore _store;
        private readonly Func<TFoo> _getNewObject;
        private readonly Mock<TFoo> _mock = new(MockBehavior.Strict);
        private readonly List<IReadOnlyList<object>> _invocationsSoFar = new();
        private readonly Func<object, string> _dumpState;
        private TFoo? _recordInstance;
        public TFoo Object => _mock.Object;

        public RecordReplay(IKeyValueStore store, Func<TFoo> getNewObject, Func<object, string> dumpState)
        {
            _store = store;
            _getNewObject = getNewObject;
            _dumpState = dumpState;
        }

        public void Setup<TArg, TReturn>(Expression<Func<TFoo, TReturn>> moq, Func<TFoo, TArg, TReturn> func)
        {
            _mock.Setup(moq).Returns(new InvocationFunc(x => RecordOrReplay(x.Arguments, func)));
        }

        private TReturn? RecordOrReplay<TArg, TReturn>(IReadOnlyList<object> args, Func<TFoo, TArg, TReturn> func)
        {
            _invocationsSoFar.Add(args);
            var hash = Hash(_invocationsSoFar);
            if (_store.TryGetValue(hash, out var val))
            {
                // Replay mode
                return (TReturn?)val;
            }

            if (_recordInstance == null)
            {
                _recordInstance = _getNewObject();
                foreach (var invocation in _invocationsSoFar.SkipLast(1))
                {
                    // Catch up previous calls
                    Invoke(func, invocation);
                }
            }

            // Record
            var result = Invoke(func, _invocationsSoFar.Last());
            _store.SetValue(hash, result);
            return result;
        }

        private TReturn Invoke<TArg, TReturn>(Func<TFoo, TArg, TReturn> func, IReadOnlyList<object> args)
        {
            return func.Invoke(_recordInstance!, (TArg)args.Single());
        }

        private string Hash(IReadOnlyList<IReadOnlyList<object>> data)
        {
            var hasher = new SHA256Managed();
            var hash = Array.Empty<byte>();

            foreach (var query in data)
            {
                foreach (var arg in query)
                {
                    hash = hasher.ComputeHash(hash.Concat(Encoding.Unicode.GetBytes(_dumpState(arg))).ToArray());
                }
            }

            return string.Join("", hash.Select(x => x.ToString("x2")));
        }
    }
}
