using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;

namespace Babbacombe.Redis.Linq {

    public interface IRedisList<T> : IList<T> {
        IDatabase Database { get; }
        string Key { get; }
        int Length { get; }
    }

    public class RedisListKey : RedisListKey<string> {
        public RedisListKey(IDatabase database, string key) : base(database, key, new StringSerializer()) { }
    }

    public class RedisListKey<T> : IRedisList<T> {
        public IDatabase Database { get; private set; }
        public string Key { get; private set; }
        private readonly ISerializer<T> _serializer;

        public RedisListKey(IDatabase database, string key, ISerializer<T> serializer = null) {
            Database = database;
            Key = key;
            _serializer = serializer ?? new JsonSerializer<T>();
        }

        public T this[int index] {
            get => _serializer.Deserialize(Database.ListGetByIndex(Key, index));
            set => Database.ListSetByIndex(Key, index, _serializer.Serialize(value));
        }

        public int Count => (int)Database.ListLength(Key);

        public int Length => (int)Database.ListLength(Key);

        public bool IsReadOnly => false;

        public void Add(T item) => Database.ListRightPush(Key, _serializer.Serialize(item));

        public void Clear() => Database.KeyDelete(Key);

        public bool Contains(T item) {
            foreach (var i in this) if (i.Equals(item)) return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            var values = Database.ListRange(Key);
            for (int i = 0; i < values.Length; i++) array[arrayIndex + i] = _serializer.Deserialize(values[i]);
        }

        public IEnumerator<T> GetEnumerator() => new RedisListKeyEnumerator<T>(Database, Key, _serializer);

        public int IndexOf(T item) {
            int pos = 0;
            foreach (var i in this) {
                if (i.Equals(item)) return pos;
                pos++;
            }
            return -1;
        }

        public void Insert(int index, T item) {
            if (index >= Length) {
                Database.ListRightPush(Key, _serializer.Serialize(item));
                return;
            }
            var pivot = Database.ListGetByIndex(Key, index);
            Database.ListInsertBefore(Key, pivot, _serializer.Serialize(item));
        }

        public bool Remove(T item) {
            return Database.ListRemove(Key, Database.ListGetByIndex(Key, IndexOf(item))) != 0;
        }

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
