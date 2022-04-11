using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public delegate bool TryConvert<TFrom, TTo>(TFrom value, out TTo result);
}
