using QModManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    class AutoSingleton<T> : MonoBehaviour where T : AutoSingleton<T>
    {
        static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GlobalObject.Instance.GetOrAddComponent<T>();

                return _instance;
            }
        }
    }
}
