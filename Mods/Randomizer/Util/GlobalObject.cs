using GRandomizer.RandomizerControllers;
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
#if DEBUG
                    _instance.AddComponent<DebugController>();
#endif
                    GameObject.DontDestroyOnLoad(_instance);
                }

                return _instance;
            }
        }

#if DEBUG
        class DebugController : MonoBehaviour
        {
            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    LootRandomizer.Instance.IncreaseDebugIndex();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    LootRandomizer.Instance.DecreaseDebugIndex();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    foreach (var s in GameObject.FindObjectsOfType<Stalker>())
                    {
                        s.LoseTooth();
                    }
                }
            }
        }
#endif
    }
}
