using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    static class PatchingExtensions
    {
        public static int GetLocalIndex(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Stloc_0)
            {
                return 0;
            }
            else if (instruction.opcode == OpCodes.Ldloc_1 || instruction.opcode == OpCodes.Stloc_1)
            {
                return 1;
            }
            else if (instruction.opcode == OpCodes.Ldloc_2 || instruction.opcode == OpCodes.Stloc_2)
            {
                return 2;
            }
            else if (instruction.opcode == OpCodes.Ldloc_3 || instruction.opcode == OpCodes.Stloc_3)
            {
                return 3;
            }
            else if (instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S || instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S ||
                     instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Stloc_S)
            {
                if (AccessTools.IsInteger(instruction.operand.GetType()))
                {
                    return Convert.ToInt32(instruction.operand);
                }
                else if (instruction.operand is LocalBuilder localBuilder)
                {
                    return localBuilder.LocalIndex;
                }
                else
                {
                    throw new NotImplementedException($"Operand of type {instruction.operand.GetType().FullName} is not implemented");
                }
            }
            else
            {
                throw new ArgumentException($"OpCode did not match Stloc* or Ldloc* OpCodes ({instruction.opcode.Name})", nameof(instruction));
            }
        }

        public static int GetArgumentIndex(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldarg_0)
            {
                return 0;
            }
            else if (instruction.opcode == OpCodes.Ldarg_1)
            {
                return 1;
            }
            else if (instruction.opcode == OpCodes.Ldarg_2)
            {
                return 2;
            }
            else if (instruction.opcode == OpCodes.Ldarg_3)
            {
                return 3;
            }
            else if (instruction.opcode == OpCodes.Ldarg || instruction.opcode == OpCodes.Ldarg_S || instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S ||
                     instruction.opcode == OpCodes.Starg || instruction.opcode == OpCodes.Starg_S)
            {
                if (AccessTools.IsInteger(instruction.operand.GetType()))
                {
                    return Convert.ToInt32(instruction.operand);
                }
                else
                {
                    throw new NotImplementedException($"Operand of type {instruction.operand.GetType().FullName} is not implemented");
                }
            }
            else
            {
                throw new ArgumentException($"OpCode did not match Starg* or Ldarg* OpCodes ({instruction.opcode.Name})", nameof(instruction));
            }
        }

        public static bool IsAny(this OpCode op, params OpCode[] opcodes)
        {
            return opcodes.Any(o => op == o);
        }

        public static Type GetMethodReturnType(this MethodBase mb)
        {
            if (mb is MethodInfo mi)
            {
                return mi.ReturnType;
            }
            else if (mb is ConstructorInfo ci)
            {
                return ci.DeclaringType;
            }
            else
            {
                throw new NotImplementedException($"{mb.GetType().FullName} is not implemented");
            }
        }

        public static IEnumerable<MethodInfo> GetImplementations(this Type interfaceType, bool allowThisAssembly, params string[] interfaceMethodNames)
        {
            MethodInfo[] methods = new MethodInfo[interfaceMethodNames.Length];
            for (int i = 0; i < methods.Length; i++)
            {
                methods[i] = AccessTools.FirstMethod(interfaceType, m => m.Name == interfaceMethodNames[i]);
            }

            return GetImplementations(interfaceType, allowThisAssembly, methods);
        }

        public static IEnumerable<MethodInfo> GetImplementations(this Type interfaceType, bool allowThisAssembly, params MethodInfo[] interfaceMethods)
        {
            int findIndex(MethodInfo[] targetMethods, MethodInfo interfaceMethod)
            {
                string explicitImplementationName = $"{interfaceType.Name}.{interfaceMethod.Name}";
                ParameterInfo[] interfaceMethodParams = interfaceMethod.GetParameters();

                return Array.FindIndex(targetMethods, m =>
                {
                    if (m.Name == interfaceMethod.Name || m.Name == explicitImplementationName)
                    {
                        if (m.ReturnType == interfaceMethod.ReturnType)
                        {
                            ParameterInfo[] parameters = m.GetParameters();
                            if (parameters.Length == interfaceMethodParams.Length)
                            {
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    if (parameters[i].ParameterType != interfaceMethodParams[i].ParameterType)
                                        return false;
                                }

                                return true;
                            }
                        }
                    }

                    return false;
                });
            }

#if VERBOSE
            bool log(string logstr)
            {
                Utils.DebugLog(logstr, false, 1);
                return true;
            }
#endif

            return from type in TypeCollection.GetAllTypes(TypeFlags.Class | (allowThisAssembly ? TypeFlags.AllAssemblies : TypeFlags.AllExceptThisAssembly))
                   where interfaceType.IsAssignableFrom(type)
                   select type.GetInterfaceMap(interfaceType) into map
                   from interfaceMethod in interfaceMethods
                   let index = findIndex(map.InterfaceMethods, interfaceMethod)
                   where index >= 0
#if VERBOSE
                   let _ = log($"Found implementation of {interfaceMethod.FullName}: {map.TargetMethods[index].FullName}")
#endif
                   select map.TargetMethods[index] into method
                   where method.HasMethodBody()
                   select method;
        }

        public static IEnumerable<CodeInstruction> HookField(this IEnumerable<CodeInstruction> instructions, FieldInfo field, MethodInfo hookMethod, HookFieldFlags flags, LocalGenerator localGen = null)
        {
            bool Ldfld = (flags & HookFieldFlags.Ldfld) != 0;
            bool Stfld = (flags & HookFieldFlags.Stfld) != 0;
            bool IncludeInstance = (flags & HookFieldFlags.IncludeInstance) != 0 && !field.IsStatic;

            if (Stfld)
            {
                if (Ldfld)
                {
                    throw new ArgumentException($"Cannot hook {nameof(HookFieldFlags.Ldfld)} and {nameof(HookFieldFlags.Stfld)} with the same method");
                }

                if (field.IsLiteral || field.IsInitOnly)
                {
                    Utils.LogWarning($"flags include {nameof(HookFieldFlags.Stfld)} but {field.Name} is readonly or const");
                    Stfld = false;
                }

                if (localGen == null)
                {
                    Utils.LogError($"flags include {nameof(HookFieldFlags.Stfld)} but {nameof(localGen)} is null");
                    Stfld = false;
                }
            }

            foreach (CodeInstruction instruction in instructions)
            {
                if ((Ldfld && instruction.LoadsField(field)) || (Stfld && instruction.StoresField(field)))
                {
                    if (IncludeInstance)
                    {
                        if (Ldfld)
                        {
                            yield return new CodeInstruction(OpCodes.Dup); // Dup instance
                        }
                        else //if (Stfld)
                        {
                            LocalBuilder newFieldValue = localGen.GetLocal(field.FieldType, false);
                            yield return new CodeInstruction(OpCodes.Stloc, newFieldValue);

                            yield return new CodeInstruction(OpCodes.Dup); // Dup instance

                            yield return new CodeInstruction(OpCodes.Ldloc, newFieldValue);
                            localGen.ReleaseLocal(newFieldValue);
                        }
                    }

                    if (Stfld)
                        yield return new CodeInstruction(OpCodes.Call, hookMethod);

                    yield return instruction;

                    if (Ldfld)
                        yield return new CodeInstruction(OpCodes.Call, hookMethod);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        struct MaterialMethods
        {
            static readonly InitializeOnAccess<MethodInfo[]> _allMethods = new InitializeOnAccess<MethodInfo[]>(() =>
            {
                return (from MethodInfo method in typeof(Material).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        where !method.IsSpecialName
                        where method.Name.StartsWith("Set") || method.Name.StartsWith("Get")
                        select method).ToArray();
            });

            static readonly InitializeOnAccess<MethodInfo[]> _getMethods = new InitializeOnAccess<MethodInfo[]>(() =>
            {
                return (from m in _allMethods.Get
                        where m.Name.StartsWith("Get")
                        select m).ToArray();
            });
            static readonly InitializeOnAccess<MethodInfo[]> _setMethods = new InitializeOnAccess<MethodInfo[]>(() =>
            {
                return (from m in _allMethods.Get
                        where m.Name.StartsWith("Set")
                        select m).ToArray();
            });

            public readonly MethodInfo GetValueString;
            public readonly MethodInfo GetValueInt;

            public readonly MethodInfo SetValueString;
            public readonly MethodInfo SetValueInt;

            MaterialMethods(MethodInfo getValueString, MethodInfo getValueInt, MethodInfo setValueString, MethodInfo setValueInt)
            {
                GetValueString = getValueString;
                GetValueInt = getValueInt;
                SetValueString = setValueString;
                SetValueInt = setValueInt;
            }

            public static MaterialMethods Create(Type type)
            {
                return new MaterialMethods
                (
                    _getMethods.Get.FindMethod(type, new Type[] { typeof(string) }),
                    _getMethods.Get.FindMethod(type, new Type[] { typeof(int) }),
                    _setMethods.Get.FindMethod(typeof(void), new Type[] { typeof(string), type }),
                    _setMethods.Get.FindMethod(typeof(void), new Type[] { typeof(int), type })
                );
            }
        }
        static readonly InitializeOnAccessDictionary<Type, MaterialMethods> _materialMethods = new InitializeOnAccessDictionary<Type, MaterialMethods>(MaterialMethods.Create);

        public static IEnumerable<CodeInstruction> HookGetMaterialValue<T>(this IEnumerable<CodeInstruction> instructions, LocalGenerator localGen, Mutator<T> valueMutator)
        {
            MaterialMethods methods = _materialMethods[typeof(T)];

            foreach (CodeInstruction instruction in instructions)
            {
                bool isIntID = instruction.Calls(methods.GetValueInt);
                if (isIntID || instruction.Calls(methods.GetValueString))
                {
                    LocalBuilder nameID = localGen.GetLocal(isIntID ? typeof(int) : typeof(string), false);

                    // Material, int/string

                    yield return new CodeInstruction(OpCodes.Stloc, nameID);

                    // Material

                    yield return new CodeInstruction(OpCodes.Dup);

                    // Material, Material

                    yield return new CodeInstruction(OpCodes.Ldloc, nameID);

                    // Material, Material, int/string

                    yield return instruction;

                    // Material, T

                    LocalBuilder value = localGen.GetLocal(typeof(T), false);

                    yield return CodeInstruction.CallClosure(valueMutator);

                    // Material, T

                    yield return new CodeInstruction(OpCodes.Stloc, value);

                    // Material

                    yield return new CodeInstruction(OpCodes.Ldloc, nameID);

                    // Material, int/string

                    yield return new CodeInstruction(OpCodes.Ldloc, value);

                    // Material, int/string, T

                    yield return new CodeInstruction(OpCodes.Callvirt, isIntID ? methods.SetValueInt : methods.SetValueString);

                    // [empty]

                    yield return new CodeInstruction(OpCodes.Ldloc, value);

                    // T

                    localGen.ReleaseLocal(nameID);
                    localGen.ReleaseLocal(value);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
