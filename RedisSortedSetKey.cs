using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Babbacombe.Redis.Linq {

    /// <summary>
    /// Interface that must be implemented by objects stored in a RedisSortedSetKey.
    /// </summary>
    public interface IScorable {
        /// <summary>
        /// Gets the score of the item.
        /// </summary>
        double Score { get; }
    }

    /// <summary>
    /// Treats a Redis Sorted Set key as a list of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisSortedSetKey<T> : IRedisList<T> where T : IScorable {
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
        /// Opens a Redis Sorted Set Key.
        /// </summary>
        /// <param name="database">The Redis database.</param>
        /// <param name="key">The Redis key name.</param>
        /// <param name="serializer">Converts the objects to and from strings. If null, a JsonSerializer is used.</param>
        public RedisSortedSetKey(IDatabase database, string key, ISerializer<T> serializer = null) {
            Database = database;
            Key = key;
            _serializer = serializer ?? new JsonSerializer<T>();
        }

        /// <summary>
        /// Gets the number of items in the Redis Sorted Set key.
        /// </summary>
        public int Count => (int)Database.SortedSetLength(Key);

        /// <summary>
        /// Gets the number of items in the Redis Sorted Set key.
        /// </summary>
        public int Length => (int)Database.SortedSetLength(Key);

        /// <summary>
        /// Always read/write.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the item at the given position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] {
            get => _serializer.Deserialize(Database.SortedSetRangeByRank(Key, index, index).FirstOrDefault());
            set => throw new NotImplementedException("Cannot add to a RedisSortedSet by setting the item's index");
        }

        /// <summary>
        /// Gets the item with the given score. If there is more than one item, the first is returned.
        /// </summary>
        /// <param name="score"></param>
        /// <returns></returns>
        public T this[double score] => _serializer.Deserialize(Database.SortedSetRangeByScore(Key, score, score).FirstOrDefault());

        /// <summary>
        /// Adds an item to the Redis Sorted Set key.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) => Database.SortedSetAdd(Key, _serializer.Serialize(item), item.Score);

        /// <summary>
        /// Adds an item to the Redis Sorted Set key. If there is already an item with the same score, it is replaced.
        /// </summary>
        /// <param name="item"></param>
        public void AddOrReplace(T item) {
            Database.SortedSetRemoveRangeByScore(Key, item.Score, item.Score);
            Add(item);
        }

        /// <summary>
        /// Adds a set of items to the Redis Sorted Set key.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<T> items) {
            foreach (var item in items) Add(item);
        }

        /// <summary>
        /// Adds a set of items. Any existing items with the same scores are replaced.
        /// </summary>
        /// <param name="items"></param>
        public void AddOrReplaceRange(IEnumerable<T> items) {
            foreach (var item in items) AddOrReplace(item);
        }

        /// <summary>
        /// Removes all the items from the Redis Sorted Set key.
        /// </summary>
        public void Clear() => Database.KeyDelete(Key);

        /// <summary>
        /// Tests whether the item is in the Redis Sorted Set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if a matching object is found.</returns>
        public bool Contains(T item) {
            double score = item.Score;
            return Database.SortedSetRangeByScore(Key, score, score).Any(i => _serializer.Deserialize(i).Equals(item));
        }

        /// <summary>
        /// Tests whether an item with the given score is in the Redis Sorted Set.
        /// </summary>
        /// <param name="score"></param>
        /// <returns>True if a matching item is found.</returns>
        public bool ContainsScore(double score) => Database.SortedSetRangeByScore(Key, score, score).Any();

        /// <summary>
        /// Gets the index of the item in the Redis Sorted Set key. -1 if not found.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item) {
            int pos = 0;
            double score = item.Score;
            foreach (var i in Database.SortedSetScan(Key)) {
                if (i.Score == score && _serializer.Deserialize(i.Element).Equals(item)) return pos;
                pos++;
            }
            return -1;
        }

        /// <summary>
        /// Not implemented. Items can only be added, as the index depends on the item's score.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <exception cref="System.NotImplementedException">Always raised.</exception>
        public void Insert(int index, T item) => throw new NotImplementedException("Can only Add items to a Redis SortedSetKey");

        /// <summary>
        /// Removes the item from the Redis Sorted Set key, if it is present.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if an item was removed.</returns>
        public bool Remove(T item) {
            var score = item.Score;
            var value = Database.SortedSetRangeByScore(Key, score, score).FirstOrDefault(i => _serializer.Deserialize(i).Equals(item));
            if (value == RedisValue.Null) return false;
            Database.SortedSetRemove(Key, value);
            return true;
        }

        /// <summary>
        /// Removes the item at the specified index in the Redis Sorted Set key.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => Database.SortedSetRemoveRangeByRank(Key, index, index);

        IEnumerator IEnumerable.GetEnumerator() => new RedisSortedSetEnumerator<T>(Database, Key, _serializer);

        /// <summary>
        /// Copies the items from the Redis Key into an array.
        /// </summary>
        /// <param name="array">The array to copy the items into.</param>
        /// <param name="arrayIndex">The index in array at which to start the copy.</param>
        public void CopyTo(T[] array, int arrayIndex) {
            var values = Database.SortedSetRangeByRank(Key);
            for (int i = 0; i < values.Length; i++) array[arrayIndex + i] = _serializer.Deserialize(values[i]);
        }

        /// <summary>
        /// Gets an enumerator for iterating over the objects in the Redis key.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => new RedisSortedSetEnumerator<T>(Database, Key, _serializer);
    }

    class RedisSortedSetEnumerator<T> : IEnumerator<T> {
        private int _pos = -1;
        private RedisValue[] _values;
        private readonly ISerializer<T> _serializer;

        public RedisSortedSetEnumerator(IDatabase db, string key, ISerializer<T> serializer) {
            _values = db.SortedSetRangeByRank(key);
            _serializer = serializer;
        }

        public T Current => _serializer.Deserialize(_values[_pos]);

        object IEnumerator.Current => Current;


        public bool MoveNext() => ++_pos < _values.Length;

        public void Reset() => _pos = -1;

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
