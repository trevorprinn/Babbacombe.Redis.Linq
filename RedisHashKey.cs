using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Babbacombe.Redis.Linq {
    public class RedisHashKey : RedisHashKey<string, string> {
        public RedisHashKey(IDatabase database, string key) : base(database, key, new StringSerializer(), new StringSerializer()) { }
    }

    public class RedisHashKey<T> : RedisHashKey<string, T> {
        public RedisHashKey(IDatabase database, string key, ISerializer<T> valueSerializer = null)
            : base(database, key, new StringSerializer(), valueSerializer) { }
    }

    public class RedisHashKey<TK, TV> : IDictionary<TK, TV> {
        public IDatabase Database { get; private set; }
        public string Key { get; private set; }
        private ISerializer<TK> _keySerializer;
        private ISerializer<TV> _valueSerializer;

        public RedisHashKey(IDatabase database, string key, ISerializer<TK> keySerializer = null, ISerializer<TV> valueSerializer = null) {
            Database = database;
            Key = key;
            _keySerializer = keySerializer ?? new JsonSerializer<TK>();
            _valueSerializer = valueSerializer ?? new JsonSerializer<TV>();
        }

        public ICollection<TK> Keys => Database.HashKeys(Key).Select(k => _keySerializer.Deserialize(k)).ToArray();

        public ICollection<TV> Values => Database.HashValues(Key).Select(v => _valueSerializer.Deserialize(v)).ToArray();

        public int Count => (int)Database.HashLength(Key);

        public bool IsReadOnly => false;

        public TV this[TK key] {
            get => _valueSerializer.Deserialize(Database.HashGet(Key, _keySerializer.Serialize(key)));
            set => Database.HashSet(Key, _keySerializer.Serialize(key), _valueSerializer.Serialize(value));
        }

        public bool ContainsKey(TK key) => Keys.Contains(key);

        public void Add(TK key, TV value) => Database.HashSet(Key, _keySerializer.Serialize(key), _valueSerializer.Serialize(value));

        public bool Remove(TK key) => Database.HashDelete(Key, _keySerializer.Serialize(key));

        public bool TryGetValue(TK key, out TV value) {
            if (ContainsKey(key)) {
                value = this[key];
                return true;
            }
            value = default(TV);
            return false;
        }

        public void Add(KeyValuePair<TK, TV> item) => Add(item.Key, item.Value);

        public void Clear() => Database.KeyDelete(Key);

        public bool Contains(KeyValuePair<TK, TV> item) => ContainsKey(item.Key);

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex) {
            int target = arrayIndex;
            foreach (var pair in Database.HashGetAll(Key)) {
                array[target++] = new KeyValuePair<TK, TV>(_keySerializer.Deserialize(pair.Name), _valueSerializer.Deserialize(pair.Value));
            }
        }

        public bool Remove(KeyValuePair<TK, TV> item) => Remove(item.Key);

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator() => new RedisHashKeyEnumerator<TK, TV>(Database, Key, _keySerializer, _valueSerializer);

        IEnumerator IEnumerable.GetEnumerator() => new RedisHashKeyEnumerator<TK, TV>(Database, Key, _keySerializer, _valueSerializer);
    }

    class RedisHashKeyEnumerator<TK, TV> : IEnumerator<KeyValuePair<TK, TV>> {
        private HashEntry[] _entries;
        private int _pos = -1;
        private ISerializer<TK> _keySerializer;
        private ISerializer<TV> _valueSerializer;

        public RedisHashKeyEnumerator(IDatabase database, string key, ISerializer<TK> keySerializer, ISerializer<TV> valueSerializer) {
            _entries = database.HashGetAll(key);
            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;
        }

        public KeyValuePair<TK, TV> Current => new KeyValuePair<TK, TV>(_keySerializer.Deserialize(_entries[_pos].Name), _valueSerializer.Deserialize(_entries[_pos].Value));

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            return ++_pos < _entries.Length;
        }

        public void Reset() {
            _pos = -1;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing) {
            _entries = null;
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
