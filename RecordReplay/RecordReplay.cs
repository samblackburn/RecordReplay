using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Castle.DynamicProxy;

namespace RecordReplay
{
    public class RecordReplay<TFoo>
        where TFoo : class
    {
        private readonly object _proxy;
        public TFoo Object => (TFoo)_proxy;

        public RecordReplay(IKeyValueStore store, Func<TFoo> getNewObject, Func<object, string>? dumpState = null)
        {
            _proxy = new ProxyGenerator().CreateInterfaceProxyWithTargetInterface(typeof(TFoo), (TFoo)null!,
                new RecordReplayInterceptor<TFoo>(store, getNewObject, dumpState));
        }
    }

    internal class RecordReplayInterceptor<TFoo> : IInterceptor
    {
        private readonly List<IInvocation> _invocationsSoFar = new();
        private readonly IKeyValueStore _store;
        private TFoo? _recordInstance;
        private readonly Func<TFoo> _getNewObject;
        private readonly Func<object, string> _dumpState;

        public RecordReplayInterceptor(IKeyValueStore store, Func<TFoo> getNewObject, Func<object, string>? dumpState)
        {
            _store = store;
            _getNewObject = getNewObject;
            _dumpState = dumpState ?? (x => JsonSerializer.Serialize(x));
        }

        public void Intercept(IInvocation invocation)
        {
            _invocationsSoFar.Add(invocation);
            var hash = Hash(_invocationsSoFar);
            if (_store.TryGetValue(hash, out var val))
            {
                // Replay mode
                invocation.ReturnValue = val;
                return;
            }

            if (_recordInstance == null)
            {
                _recordInstance = _getNewObject();
                foreach (var i in _invocationsSoFar.SkipLast(1))
                {
                    // Catch up previous calls
                    Invoke(i);
                }
            }

            // Record
            var result = Invoke(_invocationsSoFar.Last());
            _store.SetValue(hash, result);
            invocation.ReturnValue = result;
        }

        private object? Invoke(IInvocation invocation)
        {
            return invocation.Method.Invoke(_recordInstance, invocation.Arguments);
        }

        private string Hash(IEnumerable<IInvocation> invocations)
        {
            var hasher = new SHA256Managed();
            var hash = Array.Empty<byte>();

            foreach (var invocation in invocations)
            {
                hash = hasher.ComputeHash(hash.Concat(Encoding.Unicode.GetBytes(invocation.Method.ToString() ?? "")).ToArray());
                
                foreach (var arg in invocation.Arguments)
                {
                    hash = hasher.ComputeHash(hash.Concat(Encoding.Unicode.GetBytes(_dumpState(arg))).ToArray());
                }
            }

            return string.Join("", hash.Select(x => x.ToString("x2")));
        }
    }
}