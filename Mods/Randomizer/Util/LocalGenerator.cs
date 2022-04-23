using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public class LocalGenerator
    {
        readonly ILGenerator _generator;
        readonly Dictionary<Type, List<LocalBuilder>> _localsCache = new Dictionary<Type, List<LocalBuilder>>();

        public LocalGenerator(ILGenerator ilGenerator)
        {
            _generator = ilGenerator;
        }

        public LocalBuilder GetLocal(Type type, bool release = true)
        {
            if (_localsCache.TryGetValue(type, out List<LocalBuilder> localsList) && localsList.Count > 0)
                return release ? localsList[0] : localsList.GetAndRemove(0);

            LocalBuilder local = _generator.DeclareLocal(type);
            if (release)
                ReleaseLocal(local);

            return local;
        }

        public void ReleaseLocal(LocalBuilder local)
        {
            _localsCache.GetOrAddNew(local.LocalType).Add(local);
        }
    }
}
