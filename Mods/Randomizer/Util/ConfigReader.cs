using Oculus.Newtonsoft.Json;
using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace GRandomizer.Util
{
    public static class ConfigReader
    {
        static readonly Dictionary<string, JObject> _configCache = new Dictionary<string, JObject>();

        static JObject getJSONObject(string path)
        {
            const string FILE_EXTENSION = ".json";
            if (!path.EndsWith(FILE_EXTENSION))
                path += FILE_EXTENSION;

            if (_configCache.TryGetValue(path, out JObject jObj))
                return jObj;

            string fullPath = Path.Combine(Mod.ModFolder.FullName, path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException(fullPath);

            jObj = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(fullPath));
            _configCache.Add(path, jObj);
            return jObj;
        }

        public static T ReadFromFile<T>(string path)
        {
            string[] split = path.Split(new string[] { "::" }, StringSplitOptions.None);

            JObject obj = getJSONObject(split[0]);
            JToken token = obj;

            if (split.Length > 1)
            {
                string[] properties = split[1].Split('.');
                foreach (string prop in properties)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        token = token[prop];
                    }
                    else
                    {
                        throw new Exception($"Unhandled token type {token.Type}, {token.GetType().FullName}, {token}");
                    }
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
