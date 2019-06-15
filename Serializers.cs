using Newtonsoft.Json;

namespace Babbacombe.Redis.Linq {

    /// <summary>
    /// Interface that must be implemented for serializer objects that convert between strings and the objects stored in RedisListKey, RedisHashKey, or RedisSortedSetKey collections.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISerializer<T> {
        /// <summary>
        /// Serializes an object into a string.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        string Serialize(T o);
        /// <summary>
        /// Deserializes a string into an object.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        T Deserialize(string s);
    }

    /// <summary>
    /// Serializer to convert JSON strings to objects, and vice versa.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonSerializer<T> : ISerializer<T> {
        /// <summary>
        /// Serializes an object into a JSON string.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public string Serialize(T o) => JsonConvert.SerializeObject(o);
        /// <summary>
        /// Deserializes a JSON string into an object.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public T Deserialize(string s) => JsonConvert.DeserializeObject<T>(s);
    }

    /// <summary>
    /// A null serializer that "converts" strings into strings.
    /// </summary>
    public class StringSerializer : ISerializer<string> {
        /// <summary>
        /// Returns the passed in string.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public string Serialize(string o) => o;
        /// <summary>
        /// Returns the passed in string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string Deserialize(string s) => s;
    }
}
