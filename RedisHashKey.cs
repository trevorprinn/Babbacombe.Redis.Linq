using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Babbacombe.Redis.Linq {

    /// <summary>
    /// Treats a Redis Hash Key as a Dictionary of string objects with string keys.
    /// </summary>
    public class RedisHashKey : RedisHashKey<string, string> {
        /// <summary>
        /// Opens a Redis hash key.
        /// </summary>
        /// <param name="database">The Redis database.</param>
        /// <param name="key">The Redis key name.</param>
        public RedisHashKey(IDatabase database, string key) : base(database, key, new StringSerializer(), new StringSerializer()) { }
    }

    /// <summary>
    /// Treats a Redis Hash Key as a Dictionary of objects with string keys.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisHashKey<T> : RedisHashKey<string, T> {
        /// <summary>
        /// Opens a Redis hash key.
        /// </summary>
        /// <param name="database">The Redis database.</param>
        /// <param name="key">The Redis key name.</param>
        /// <param name="valueSerializer">Converts the value objects to and from strings. If null, a JsonSerializer is used.</param>
        public RedisHashKey(IDatabase database, string key, ISerializer<T> valueSerializer = null)
            : base(database, key, new StringSerializer(), valueSerializer) { }
    }

    /// <summary>
    /// Treats a Redis Hash Key as a Dictionary of objects with keys that are objects.
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    public class RedisHashKey<TK, TV> : IDictionary<TK, TV> {
        /// <summary>
        /// The database containing the key
        /// </summary>
        public IDatabase Database { get; private set; }
        /// <summary>
        /// The Redis Key name.
        /// </summary>
        public string Key { get; private set; }
        private readonly ISerializer<TK> _keySerializer;
        private readonly ISerializer<TV> _valueSerializer;

        /// <summary>
        /// Opens a Redis hash key.
        /// </summary>
        /// <param name="database">The Redis database.</param>
        /// <param name="key">The Redis key name.</param>
        /// <param name="keySerializer">Converts the key objects to and from strings. If null, a JsonSerializer is used.</param>
        /// <param name="valueSerializer">Converts the value objects to and from strings. If null, a JsonSerializer is used.</param>
        public RedisHashKey(IDatabase database, string key, ISerializer<TK> keySerializer = null, ISerializer<TV> valueSerializer = null) {
            Database = database;
            Key = key;
            _keySerializer = keySerializer ?? new JsonSerializer<TK>();
            _valueSerializer = valueSerializer ?? new JsonSerializer<TV>();
        }

        /// <summary>
        /// The keys within the Redis Hash key.
        /// </summary>
        public ICollection<TK> Keys => Database.HashKeys(Key).Select(k => _keySerializer.Deserialize(k)).ToArray();

        /// <summary>
        /// The values within the Redis Hash key.
        /// </summary>
        public ICollection<TV> Values => Database.HashValues(Key).Select(v => _valueSerializer.Deserialize(v)).ToArray();

        /// <summary>
        /// The count of items within the Redis Hash key.
        /// </summary>
        public int Count => (int)Database.HashLength(Key);

        /// <summary>
        /// Always Read/Write.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Sets or returns a value with the given key.
        /// </summary>
        /// <param name="key">The item key within the Redis key</param>
        /// <returns></returns>
        public TV this[TK key] {
            get => _valueSerializer.Deserialize(Database.HashGet(Key, _keySerializer.Serialize(key)));
            set => Database.HashSet(Key, _keySerializer.Serialize(key), _valueSerializer.Serialize(value));
        }

        /// <summary>
        /// True if the Redis Key contains an item with the given key.
        /// </summary>
        /// <param name="key">The item key within the Redis key.</param>
        /// <returns></returns>
        public bool ContainsKey(TK key) => Keys.Contains(key);

        /// <summary>
        /// Adds the item to the Redis key.
        /// </summary>
        /// <param name="key">The item key within the Redis key.</param>
        /// <param name="value">The item to add.</param>
        public void Add(TK key, TV value) => Database.HashSet(Key, _keySerializer.Serialize(key), _valueSerializer.Serialize(value));

        /// <summary>
        /// Removes any item with the given key from the Redis key.
        /// </summary>
        /// <param name="key">The key of the item to remove.</param>
        /// <returns></returns>
        public bool Remove(TK key) => Database.HashDelete(Key, _keySerializer.Serialize(key));

        /// <summary>
        /// Attempts to retrieve a value from the Redis key.
        /// </summary>
        /// <param name="key">The key of the item within the Redis key to retrieve.</param>
        /// <param name="value">The value retrieved, if present.</param>
        /// <returns>True if an item with the key exists within the Redis key.</returns>
        public bool TryGetValue(TK key, out TV value) {
            if (ContainsKey(key)) {
                value = this[key];
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Adds the item to the Redis key.
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<TK, TV> item) => Add(item.Key, item.Value);

        /// <summary>
        /// Deletes all of the items in the Redis Key.
        /// </summary>
        public void Clear() => Database.KeyDelete(Key);

        /// <summary>
        /// Tests whether the item exists in the Redis key with a particular key.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<TK, TV> item) => ContainsKey(item.Key) && this[item.Key].Equals(item.Value);

        /// <summary>
        /// Copies the key/value pairs from the Redis Key into an array.
        /// </summary>
        /// <param name="array">The array to copy the key/value pairs into.</param>
        /// <param name="arrayIndex">The index in array at which to start the copy.</param>
        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex) {
            int target = arrayIndex;
            foreach (var pair in Database.HashGetAll(Key)) {
                array[target++] = new KeyValuePair<TK, TV>(_keySerializer.Deserialize(pair.Name), _valueSerializer.Deserialize(pair.Value));
            }
        }

        /// <summary>
        /// Removes the value from the Redis key if it is keyed on the given key.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<TK, TV> item) {
            if (Contains(item)) {
                Remove(item.Key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets an enumerator for iterating over the key/value pairs in the Redis key.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator() => new RedisHashKeyEnumerator<TK, TV>(Database, Key, _keySerializer, _valueSerializer);

        IEnumerator IEnumerable.GetEnumerator() => new RedisHashKeyEnumerator<TK, TV>(Database, Key, _keySerializer, _valueSerializer);
    }

    class RedisHashKeyEnumerator<TK, TV> : IEnumerator<KeyValuePair<TK, TV>> {
        private HashEntry[] _entries;
        private int _pos = -1;
        private readonly ISerializer<TK> _keySerializer;
        private readonly ISerializer<TV> _valueSerializer;

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
