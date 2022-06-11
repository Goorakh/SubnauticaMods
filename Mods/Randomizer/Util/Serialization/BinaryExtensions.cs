using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GRandomizer.Util.Serialization
{
    public static class BinaryExtensions
    {
        // TODO: Support finding and constructing generic methods
        static readonly InitializeOnAccessDictionary<Type, MethodInfo> _writeMethodByType = new InitializeOnAccessDictionary<Type, MethodInfo>(type =>
        {
            foreach (MethodInfo binaryWriterMethod in AccessTools.GetDeclaredMethods(typeof(BinaryWriter)))
            {
                if (binaryWriterMethod.IsPublic && binaryWriterMethod.ReturnType == typeof(void) && binaryWriterMethod.Name == "Write")
                {
                    ParameterInfo[] parameters = binaryWriterMethod.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == type)
                    {
                        return binaryWriterMethod;
                    }
                }
            }

            foreach (MethodInfo extensionMethod in AccessTools.GetDeclaredMethods(typeof(SystemExtensions))
                                                              .Concat(AccessTools.GetDeclaredMethods(typeof(BinaryExtensions))))
            {
                if (extensionMethod.IsPublic && extensionMethod.GetCustomAttributes().Any(attr => attr.GetType().Name == "ExtensionAttribute"))
                {
                    if (extensionMethod.Name.StartsWith("Write") && extensionMethod.ReturnType == typeof(void))
                    {
                        ParameterInfo[] parameters = extensionMethod.GetParameters();
                        if (parameters.Length == 2 && parameters[0].ParameterType == typeof(BinaryWriter) && parameters[1].ParameterType == type)
                        {
                            return extensionMethod;
                        }
                    }
                }
            }

            throw new Exception($"No write method could be found for {type.FullDescription()}");
        });

        static readonly InitializeOnAccessDictionary<Type, MethodInfo> _readMethodByType = new InitializeOnAccessDictionary<Type, MethodInfo>(type =>
        {
            foreach (MethodInfo binaryReaderMethod in AccessTools.GetDeclaredMethods(typeof(BinaryReader)))
            {
                if (binaryReaderMethod.IsPublic && binaryReaderMethod.ReturnType == type && binaryReaderMethod.Name != "Read" && binaryReaderMethod.Name.StartsWith("Read"))
                {
                    if (binaryReaderMethod.GetParameters().Length == 0)
                    {
                        return binaryReaderMethod;
                    }
                }
            }

            foreach (MethodInfo extensionMethod in AccessTools.GetDeclaredMethods(typeof(SystemExtensions))
                                                              .Concat(AccessTools.GetDeclaredMethods(typeof(BinaryExtensions))))
            {
                if (extensionMethod.IsPublic && extensionMethod.GetCustomAttributes().Any(attr => attr.GetType().Name == "ExtensionAttribute"))
                {
                    if (extensionMethod.Name.StartsWith("Read") && extensionMethod.ReturnType == type)
                    {
                        ParameterInfo[] parameters = extensionMethod.GetParameters();
                        if (parameters.Length == 1 && typeof(BinaryReader).IsAssignableFrom(parameters[0].ParameterType))
                        {
                            return extensionMethod;
                        }
                    }
                }
            }

            throw new Exception($"No read method could be found for {type.FullDescription()}");
        });

        public static void WriteGeneric<T>(this BinaryWriter writer, T value)
        {
            Type type = typeof(T);

            if (type.IsClass || (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                // This monstrosity just means `value != null && !value.Equals(null)`, but the == opererator can't be used for generic types
                if (!writer.WriteAndReturn(!(value?.Equals(null) ?? true)))
                {
                    return;
                }
            }

            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            MethodInfo writeMethod = _writeMethodByType[type];
#if VERBOSE
            Utils.DebugLog($"{typeof(T).FullDescription()}: {writeMethod.FullDescription()}");
#endif
            if (writeMethod.IsStatic)
            {
                writeMethod.Invoke(null, new object[] { writer, value });
            }
            else
            {
                writeMethod.Invoke(writer, new object[] { value });
            }
        }
        public static T ReadGeneric<T>(this VersionedBinaryReader reader)
        {
            Type type = typeof(T);

            if (reader.Version >= SaveVersion.v0_0_2_0c)
            {
                if (type.IsClass || (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    if (!reader.ReadBoolean())
                    {
                        return default(T);
                    }
                }
            }

            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            MethodInfo readMethod = _readMethodByType[type];
#if VERBOSE
            Utils.DebugLog($"{typeof(T).FullDescription()}: {readMethod.FullDescription()}");
#endif
            if (readMethod.IsStatic)
            {
                return (T)readMethod.Invoke(null, new object[] { reader });
            }
            else
            {
                return (T)readMethod.Invoke(reader, null);
            }
        }

        public static void Write<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> dict)
        {
            writer.Write(dict.Count);

#if VERBOSE
            Utils.DebugLog($"Wrote count: {dict.Count}");
#endif

            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                writer.WriteGeneric(kvp.Key);
                writer.WriteGeneric(kvp.Value);

#if VERBOSE
                Utils.DebugLog($"Wrote key: {kvp.Key}");
                Utils.DebugLog($"Wrote value: {kvp.Value}");
#endif
            }
        }
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this VersionedBinaryReader reader)
        {
            IEnumerable<KeyValuePair<TKey, TValue>> read()
            {
                int count = reader.ReadInt32();

#if VERBOSE
                Utils.DebugLog($"Read count: {count}");
#endif

                for (int i = 0; i < count; i++)
                {
                    TKey key = reader.ReadGeneric<TKey>();
                    TValue value = reader.ReadGeneric<TValue>();

#if VERBOSE
                    Utils.DebugLog($"Read key: {key}");
                    Utils.DebugLog($"Read value: {value}");
#endif

                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }

            return read().ToDictionary();
        }

        public static ReplacementDictionary<T> ReadReplacementDictionary<T>(this VersionedBinaryReader reader)
        {
            return new ReplacementDictionary<T>(reader.ReadDictionary<T, T>());
        }

        public static void Write(this BinaryWriter writer, Vector2int vector2int)
        {
            writer.Write(vector2int.x);
            writer.Write(vector2int.y);
        }
        public static Vector2int ReadVector2int(this VersionedBinaryReader reader)
        {
            return new Vector2int(reader.ReadInt32(), reader.ReadInt32());
        }

        public static void Write<T>(this BinaryWriter writer, T[] array)
        {
            writer.Write(array.Length);

            foreach (T item in array)
            {
                writer.WriteGeneric(item);
            }
        }
        public static T[] ReadArray<T>(this VersionedBinaryReader reader)
        {
            int length = reader.ReadInt32();
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = reader.ReadGeneric<T>();
            }

            return array;
        }

        public static T WriteAndReturn<T>(this BinaryWriter writer, T value)
        {
            writer.WriteGeneric(value);
            return value;
        }
    }
}
