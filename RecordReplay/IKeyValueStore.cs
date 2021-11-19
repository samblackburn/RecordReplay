using System.Collections.Generic;

namespace RecordReplay
{
    public interface IKeyValueStore
    {
        bool TryGetValue(string hash, out object? val);
        void SetValue(string hash, object? val);
    }

    internal class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, object?> _state = new();
        public bool TryGetValue(string hash, out object? val) => _state.TryGetValue(hash, out val);
        public void SetValue(string hash, object? val) => _state[hash] = val;
    }
}
