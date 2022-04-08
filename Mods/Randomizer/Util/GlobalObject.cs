using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    static class GlobalObject
    {
        static GameObject _instance;
        public static GameObject Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("GRandomizer_GlobalObject");
                    GameObject.DontDestroyOnLoad(_instance);
                }

                return _instance;
            }
        }
    }
}
