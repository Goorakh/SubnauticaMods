using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    static class TypeCollection
    {
        static InitializeOnAccess<Type[]> _allTypes = new InitializeOnAccess<Type[]>(() =>
        {
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    select type).ToArray();
        });

        static InitializeOnAccess<Type[]> _allTypesExceptThisAssembly = new InitializeOnAccess<Type[]>(() =>
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            return _allTypes.Get.Where(t => t.Assembly != thisAssembly).ToArray();
        });

        static InitializeOnAccess<Type[]> _allTypesThisAssembly = new InitializeOnAccess<Type[]>(() =>
        {
            return Assembly.GetExecutingAssembly().GetTypes();
        });

        static InitializeOnAccessDictionary<TypeFlags, Type[]> _typesByFilter = new InitializeOnAccessDictionary<TypeFlags, Type[]>(flags =>
        {
            IEnumerable<Type> typesBase;
            if ((flags & TypeFlags.AllAssemblies) != 0)
            {
                typesBase = _allTypes.Get;
            }
            else if ((flags & TypeFlags.OtherAssemblies) != 0)
            {
                typesBase = _allTypesExceptThisAssembly.Get;
            }
            else if ((flags & TypeFlags.ThisAssembly) != 0)
            {
                typesBase = _allTypesThisAssembly.Get;
            }
            else
            {
                return Array.Empty<Type>();
            }

            if ((flags & TypeFlags.Interface) != 0)
                typesBase = typesBase.Where(t => t.IsInterface);

            if ((flags & TypeFlags.Class) != 0)
                typesBase = typesBase.Where(t => t.IsClass);

            if ((flags & TypeFlags.ValueType) != 0)
                typesBase = typesBase.Where(t => t.IsValueType);

            return typesBase.ToArray();
        });

        public static Type[] GetAllTypes(TypeFlags typeFlags)
        {
            return _typesByFilter[typeFlags];
        }

        public static void Clear()
        {
            _allTypes = null;
            _allTypesExceptThisAssembly = null;
            _typesByFilter = null;
        }
    }
}
