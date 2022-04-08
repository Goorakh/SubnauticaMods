using Oculus.Newtonsoft.Json;
using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public static class ConfigReader
    {
        static readonly Dictionary<string, JObject> _configCache = new Dictionary<string, JObject>();

        static JObject getJSONObject(string path)
        {
            if (_configCache.TryGetValue(path, out JObject cached))
                return cached;

            string fullPath = Path.Combine(Mod.ModFolder.FullName, path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException(fullPath);

            JObject obj = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(fullPath));
            _configCache.Add(path, obj);
            return obj;
        }

        public static T ReadFromFile<T>(string path)
        {
            string[] split = path.Split(new string[] { "::" }, StringSplitOptions.None);

            JObject obj = getJSONObject(split[0]);
            JToken token = obj;

            string[] properties = split[1].Split('.');
            foreach (string prop in properties)
            {
                if (token.Type == JTokenType.Object)
                {
                    token = ((JObject)token)[prop];
                }
                else
                {
                    throw new Exception($"Unhandled token type {token.Type}, {token.GetType().FullName}, {token}");
                }
            }

            if (typeof(JToken).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)token;
            }
            else
            {
                return token.ToObject<T>();
            }
        }
    }
}
