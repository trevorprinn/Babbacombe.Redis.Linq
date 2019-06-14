using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

namespace Babbacombe.Redis.Linq {

    public interface IScorable {
        double Score { get; }
    }

    public class RedisSortedSetKey<T> : IList<T> where T : IScorable {
        public IDatabase Database { get; private set; }
        public string Key { get; private set; }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        private readonly ISerializer<T> _serializer;

        public RedisSortedSetKey(IDatabase database, string key, ISerializer<T> serializer = null) {
            Database = database;
            Key = key;
            _serializer = serializer ?? new JsonSerializer<T>();
        }

        public T this[int index] {
            get => _serializer.Deserialize(Database.SortedSetRangeByRank(Key, index, index).FirstOrDefault());
            set => throw new NotImplementedException("Cannot add to a RedisSortedSet by setting the item's index");
        }

        public T this[double score] => _serializer.Deserialize(Database.SortedSetRangeByScore(Key, score, score).First());

        public void Add(T item) => Database.SortedSetAdd(Key, _serializer.Serialize(item), item.Score);

        public void AddOrReplace(T item) {
            Database.SortedSetRemoveRangeByScore(Key, item.Score, item.Score);
            Add(item);
        }

        public void AddRange(IEnumerable<T> items) {
            foreach (var item in items) Add(item);
        }

        public void AddOrReplaceRange(IEnumerable<T> items) {
            foreach (var item in items) AddOrReplace(item);
        }

        public void Clear() => Database.KeyDelete(Key);

        public bool Contains(T item) {
            double score = item.Score;
            return Database.SortedSetRangeByScore(Key, score, score).Any(i => _serializer.Deserialize(i).Equals(item));
        }

        public bool ContainsScore(double score) => Database.SortedSetRangeByScore(Key, score, score).Any();

        public int IndexOf(T item) {
            int pos = 0;
            double score = item.Score;
            foreach (var i in Database.SortedSetScan(Key)) {
                if (i.Score == score && _serializer.Deserialize(i.Element).Equals(item)) return pos;
                pos++;
            }
            return -1;
        }

        public void Insert(int index, T item) => throw new NotImplementedException("Can only Add items to a Redis SortedSetKey");

        public bool Remove(T item) {
            var score = item.Score;
            var value = Database.SortedSetRangeByScore(Key, score, score).FirstOrDefault(i => _serializer.Deserialize(i).Equals(item));
            if (value == RedisValue.Null) return false;
            Database.SortedSetRemove(Key, value);
            return true;
        }

        public void RemoveAt(int index) => Database.SortedSetRemoveRangeByRank(Key, index, index);

        IEnumerator IEnumerable.GetEnumerator() => new RedisSortedSetEnumerator<T>(Database, Key, _serializer);

        public void CopyTo(T[] array, int arrayIndex) {
            var values = Database.SortedSetRangeByRank(Key);
            for (int i = 0; i < values.Length; i++) array[arrayIndex + i] = _serializer.Deserialize(values[i]);
        }

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
