using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Catotopia.Defs;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using Catotopia.Shared;

namespace Catotopia
{
    public class Resources : IResourcesContainer
    {
        private static Resources _res = new Resources();

        private readonly string _root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/");
        private Dictionary<string, IDef> _resources = new Dictionary<string, IDef>();
        private Dictionary<string, Type> _types = new Dictionary<string, Type>();

        private Resources()
        {
            foreach (var filename in Directory.GetFiles(_root, "*.json", SearchOption.AllDirectories))
                _resources.Add(filename.Substring(_root.Length, filename.Length - _root.Length - 5).Replace('\\', '/'), null);
        }

        private string LoadJsonFromFile(string path)
        {
            return File.ReadAllText(Path.ChangeExtension(Path.Combine(_root, path), ".json"));
        }

        private Type GetType(string typeName)
        {
            Type type;
            if (!_types.TryGetValue(typeName, out type))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName, false);
                    if (type != null)
                    {
                        _types.Add(typeName, type);
                        break;
                    }
                }
            }
            return type;
        }

        private TResult InvokeGeneric<TResult>(string methodName, Type genericParam, params object[] parameters)
        {
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(genericParam);
            return (TResult)genericMethod.Invoke(this, parameters);
        }

        private T InvokeGeneric<T>(string methodName, params object[] parameters)
        {
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(typeof(T));
            return (T)genericMethod.Invoke(this, parameters);
        }

        public static T Get<T>(string path) where T :class, IDef
        {
            return _res.GetInternal<T>(path);
        }

        private T GetInternal<T>(string path) where T : class, IDef
        {
            IDef result;
            if (_resources.TryGetValue(path, out result))
            {
                if (result != null)
                    return result as T;
                result = GetDefFrom<T>(JToken.Parse(LoadJsonFromFile(path)));
                _resources[path] = result;
                return result as T;
            }
            throw new KeyNotFoundException($"Resource '{path}' isnt exist");
        }

        public T GetFrom<T>(JToken source, string field)
        {
            var token = source[field];
            if (token == null)
                return default(T);
            return InvokeGeneric<T>(nameof(GetUniversalFrom), token);
        }

        private T GetUniversalFrom<T>(JToken token)
        {
            var type = typeof(T);

            if (type == typeof(int) ||
                type == typeof(float) ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type == typeof(long) ||
                type == typeof(byte) ||
                type == typeof(double) ||
                type == typeof(short) ||
                type == typeof(char))
                return GetSimpleFrom<T>(token);
            else if (typeof(IDef).IsAssignableFrom(type))
                return GetDefFrom<T>(token);
            else if (type.IsArray)
            {
                switch (type.GetArrayRank())
                {
                    case 1: return InvokeGeneric<T>(nameof(GetArrayFrom), type.GetElementType(), token);
                    case 2: return InvokeGeneric<T>(nameof(GetArray2From), type.GetElementType(), token);
                    case 3: return InvokeGeneric<T>(nameof(GetArray3From), type.GetElementType(), token);
                }
            }
            throw new ArgumentException();
        }

        private T GetDefFrom<T>(JToken token)
        {
            if (token.Type == JTokenType.String)
                return InvokeGeneric<T>(nameof(GetInternal), token.Value<string>());
            if (token.Type == JTokenType.Null)
                return default(T);

                var type = GetType(token["$type"].Value<string>());
            return InvokeGeneric<T>(nameof(GetStrongDefFrom), type, token);
        }

        private T GetStrongDefFrom<T>(JToken token) where T : class, IDef
        {
            var def = Activator.CreateInstance<T>();
            def.Fill(token, this as IResourcesContainer);
            return def;
        }

        private T GetSimpleFrom<T>(JToken token)
        {
            return token.Value<T>();
        }

        private T[] GetArrayFrom<T>(JToken token)
        {
            if (token.Type != JTokenType.Array)
                throw new ArgumentException();
            return token.Select(t => GetUniversalFrom<T>(t)).ToArray();
        }

        private T[,] GetArray2From<T>(JToken token)
        {
            if (token.Type != JTokenType.Array)
                throw new ArgumentException();
            var arrayToken = token as JArray;
            var height = arrayToken.Count;
            if (height == 0)
                return new T[0, 0];
            var width = (arrayToken[0] as JArray).Count;
            var result = new T[width, height];
            for (int y = 0; y < height; y++)
            {
                var lineToken = arrayToken[y] as JArray;
                for (int x = 0; x < width; x++)
                    result[x, y] = GetUniversalFrom<T>(lineToken[x]);
            }
            return result;
        }

        private T[,,] GetArray3From<T>(JToken token)
        {
            if (token.Type != JTokenType.Array)
                throw new ArgumentException();
            var arrayToken = token as JArray;
            var depth = arrayToken.Count;
            if (depth == 0)
                return new T[0, 0, 0];
            var height = (arrayToken[0] as JArray).Count;
            if (height == 0)
                return new T[0, 0, 0];
            var width = (arrayToken[0][0] as JArray).Count;
            var result = new T[width, height, depth];
            for (int z = 0; z < depth; z++)
            {
                var planeToken = arrayToken[z] as JArray;
                for (int y = 0; y < height; y++)
                {
                    var lineToken = planeToken[y] as JArray;
                    for (int x = 0; x < width; x++)
                        result[x, y, z] = GetUniversalFrom<T>(lineToken[x]);
                }
            }
            return result;
        }
    }
}
