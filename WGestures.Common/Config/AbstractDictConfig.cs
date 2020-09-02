using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WGestures.Common.Config
{
    public abstract class AbstractDictConfig : IConfig
    {
        protected Dictionary<string, object> Dict { get; set; } = new Dictionary<string, object>();

        public T Get<T>(string key)
        {
            object val;
            if (this.Dict.TryGetValue(key, out val))
            {
                return (T) val;
            }

            var t = Nullable.GetUnderlyingType(typeof(T));
            if (t != null)
            {
                return default;
            }

            throw new KeyNotFoundException(key);
        }

        public T Get<T>(string key, T defaultValue)
        {
            try
            {
                return this.Get<T>(key);
            }
            catch (KeyNotFoundException)
            {
                return defaultValue;
            }
        }

        public void Set<T>(string key, T value)
        {
            this.Dict[key] = value;
        }

        public abstract void Save();

        public void Import(params IConfig[] from)
        {
            if (from == null || !from.Any())
            {
                return;
            }

            foreach (var config in from)
            {
                if (config == null)
                {
                    continue;
                }

                foreach (var kv in config)
                {
                    this.Set(kv.Key, kv.Value);
                }
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this.Dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Dict.GetEnumerator();
        }

        public bool IsSet(string key)
        {
            return this.Dict.ContainsKey(key);
        }
    }
}
