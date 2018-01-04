using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babbacombe.Redis.Linq {

    public interface ISerializer<T> {
        string Serialize(T o);
        T Deserialize(string s);
    }

    public class JsonSerializer<T> : ISerializer<T> {
        public string Serialize(T o) => JsonConvert.SerializeObject(o);
        public T Deserialize(string s) => JsonConvert.DeserializeObject<T>(s);
    }

    public class StringSerializer : ISerializer<string> {
        public string Serialize(string o) => o;
        public string Deserialize(string s) => s;
    }
}
