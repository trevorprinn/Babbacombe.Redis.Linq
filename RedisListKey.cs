using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;

namespace Babbacombe.Redis.Linq {

    /// <summary>
    /// Interface implemented by RedisListKey and RedisSortedSetKey.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRedisList<T> : IList<T> {
        /// <summary>
        /// The Redis database.
        /// </summary>
        IDatabase Database { get; }
        /// <summary>
        /// The Redis Key name.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// The number of values in the list.
        /// </summary>
        int Length { get; }
    }

    /// <summary>
    /// Treats a Redis List Key as a list of string values.
    /// </summary>
    public class RedisListKey : RedisListKey<string> {
        /// <summary>
        /// Opens a Redis List key.
        /// </summary>
        /// <param name="database">The Redis database.</param>
        /// <param name="key">The Redis Key name.</param>
        public RedisListKey(IDatabase database, string key) : base(database, key, new StringSerializer()) { }
    }

    /// <summary>
    /// Treats a Redis List Key as a list of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisListKey<T> : IRedisList<T> {
        /// <summary>
        /// The Redis database.
        /// </summary>
        public IDatabase Database { get; private set; }
        /// <summary>
        /// The Redis key name.
        /// </summary>
        public string Key { get; private set; }
        private readonly ISerializer<T> _serializer;

        /// <summary>
        /// Opens a Redis List Key.
        /// </summary>
        /// <param name="database">The Redis database.</param>
        /// <param name="key">The Redis key name.</param>
        /// <param name="serializer">Converts the objects to and from strings. If null, a JsonSerializer is used.</param>
        public RedisListKey(IDatabase database, string key, ISerializer<T> serializer = null) {
            Database = database;
            Key = key;
            _serializer = serializer ?? new JsonSerializer<T>();
        }

        /// <summary>
        /// Gets or sets the item at the given position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] {
            get => _serializer.Deserialize(Database.ListGetByIndex(Key, index));
            set => Database.ListSetByIndex(Key, index, _serializer.Serialize(value));
        }

        /// <summary>
        /// Gets the number of items in the Redis List key.
        /// </summary>
        public int Count => (int)Database.ListLength(Key);

        /// <summary>
        /// Gets the number of items in the Redis List key.
        /// </summary>
        public int Length => (int)Database.ListLength(Key);

        /// <summary>
        /// Always read/write.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the end of the Redis key.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) => Database.ListRightPush(Key, _serializer.Serialize(item));

        /// <summary>
        /// Removes all the items from the Redis key.
        /// </summary>
        public void Clear() => Database.KeyDelete(Key);

        /// <summary>
        /// Tests whether an item is in the Redis List key.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if a matching object is found.</returns>
        public bool Contains(T item) {
            foreach (var i in this) if (i.Equals(item)) return true;
            return false;
        }

        /// <summary>
        /// Copies the items from the Redis Key into an array.
        /// </summary>
        /// <param name="array">The array to copy the items into.</param>
        /// <param name="arrayIndex">The index in array at which to start the copy.</param>
        public void CopyTo(T[] array, int arrayIndex) {
            var values = Database.ListRange(Key);
            for (int i = 0; i < values.Length; i++) array[arrayIndex + i] = _serializer.Deserialize(values[i]);
        }

        /// <summary>
        /// Gets an enumerator for iterating over the objects in the Redis key.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => new RedisListKeyEnumerator<T>(Database, Key, _serializer);

        /// <summary>
        /// Gets the index of the item in the Redis List key. -1 if not found.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item) {
            int pos = 0;
            foreach (var i in this) {
                if (i.Equals(item)) return pos;
                pos++;
            }
            return -1;
        }

        /// <summary>
        /// Inserts an item into the Redis List key at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item) {
            if (index >= Length) {
                Database.ListRightPush(Key, _serializer.Serialize(item));
                return;
            }
            var pivot = Database.ListGetByIndex(Key, index);
            Database.ListInsertBefore(Key, pivot, _serializer.Serialize(item));
        }

        /// <summary>
        /// Removes the item from the Redis List key, if it is present.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if an item was removed.</returns>
        public bool Remove(T item) {
            return Database.ListRemove(Key, Database.ListGetByIndex(Key, IndexOf(item))) != 0;
        }

        /// <summary>
        /// Removes the item at the specified index in the Redis List key.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) {
            Database.ListRemove(Key, Database.ListGetByIndex(Key, index));
        }

        IEnumerator IEnumerable.GetEnumerator() => new RedisListKeyEnumerator<T>(Database, Key, _serializer);
    }

    class RedisListKeyEnumerator<T> : IEnumerator<T> {
        private int _pos = -1;
        private RedisValue[] _values;
        private readonly ISerializer<T> _serializer;

        public RedisListKeyEnumerator(IDatabase db, string key, ISerializer<T> serializer) {
            _values = db.ListRange(key, 0, db.ListLength(key) - 1);
            _serializer = serializer;
        }

        public T Current => _serializer.Deserialize(_values[_pos]);

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            return ++_pos < _values.Length;
        }

        public void Reset() {
            _pos = -1;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing) {
            _values = null;
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }


}
