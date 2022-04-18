using FMOD;
using FMOD.Studio;
using FMODUnity;
using GRandomizer.RandomizerControllers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GRandomizer.Util
{
    public class GlobalObject : MonoBehaviour
    {
        struct ScheduledAction
        {
            public readonly Action Callback;
            public readonly float ExecuteTime;

            public ScheduledAction(Action callback, float executeTime)
            {
                Callback = callback;
                ExecuteTime = executeTime;
            }
        }
        static readonly List<ScheduledAction> _schedulesActions = new List<ScheduledAction>();

        static GameObject _instance;
        public static GameObject Instance
        {
            get
            {
                CreateIfMissing();
                return _instance;
            }
        }

        public static void CreateIfMissing()
        {
            if (_instance == null)
            {
                _instance = new GameObject("GRandomizer_GlobalObject");
#if DEBUG
                _instance.AddComponent<DebugController>();
#endif
                GameObject.DontDestroyOnLoad(_instance);
            }
        }

        public static void RunNextFrame(Action callback)
        {
            Schedule(callback, 0f);
        }

        public static void Schedule(Action callback, float waitTime)
        {
            CreateIfMissing();
            _schedulesActions.Add(new ScheduledAction(callback, Time.time + waitTime));
        }

        void Update()
        {
            for (int i = _schedulesActions.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _schedulesActions[i].ExecuteTime)
                {
                    _schedulesActions[i].Callback();
                    _schedulesActions.RemoveAt(i);
                }
            }
        }

#if DEBUG
        class DebugController : MonoBehaviour
        {
            private void OnConsoleCommand_r_play(NotificationCenter.Notification n)
            {
                string arg = (string)n.data[0];
                Utils.DebugLog(arg, true);
                foreach (KeyValuePair<string, RuntimeManager.LoadedBank> item in RuntimeManager.Instance.loadedBanks)
                {
                    if (completedBanks.Contains(item.Key))
                        continue;

                    RESULT result;
                    if ((result = item.Value.Bank.getEventList(out EventDescription[] events)) == RESULT.OK)
                    {
                        foreach (EventDescription ev in events)
                        {
                            RESULT pathres;
                            if ((pathres = ev.getPath(out string path)) == RESULT.OK)
                            {
                                if (path.StartsWith("event:/"))
                                {
                                    if (arg == path)
                                    {
                                        FMODUWE.GetEventInstance(path, out EventInstance instance);
                                        instance.setVolume(1f);
                                        instance.set3DAttributes(Camera.main.transform.position.To3DAttributes());
                                        instance.start();
                                        playedSounds.Add(instance);

                                        Utils.DebugLog($"Playing {path} from bank:/{item.Key}", true);
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                Utils.DebugLog($"Error: EventDescription.getPath returned {pathres}", true);
                            }
                        }
                    }
                    else
                    {
                        Utils.DebugLog($"Error: Bank.getEventList returned {result}", true);
                    }
                }
            }

            bool tmp = false;

            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    DevConsole.RegisterConsoleCommand(this, "r_play", true, true);
                }

                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    LootRandomizer.IncreaseDebugIndex();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    LootRandomizer.DecreaseDebugIndex();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    Settings settings = FMODUnity.Settings.Instance;
                    foreach (string text in settings.MasterBanks)
                    {
                        RuntimeManager.LoadBank(text + ".strings", settings.AutomaticSampleLoading);
                        if (settings.AutomaticEventLoading)
                        {
                            RuntimeManager.LoadBank(text, settings.AutomaticSampleLoading);
                        }
                    }
                    if (settings.AutomaticEventLoading)
                    {
                        foreach (string bankName in settings.Banks)
                        {
                            RuntimeManager.LoadBank(bankName, settings.AutomaticSampleLoading);
                        }
                        RuntimeManager.WaitForAllLoads();
                    }

                    tmp = true;
                }
                else if (tmp && !RuntimeManager.AnyBankLoading())
                {
                    tmp = false;

                    Utils.DebugLog("Banks loaded!", true);
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    foreach (EventInstance sound in playedSounds)
                    {
                        sound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        sound.release();
                    }

                    playedSounds.Clear();
                }
                else if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    foreach (KeyValuePair<string, RuntimeManager.LoadedBank> item in RuntimeManager.Instance.loadedBanks)
                    {
                        if (completedBanks.Contains(item.Key))
                            continue;

                        RESULT result;
                        if ((result = item.Value.Bank.getEventList(out EventDescription[] events)) == RESULT.OK)
                        {
                            bool foundLast = false;
                            foreach (EventDescription ev in events)
                            {
                                RESULT pathres;
                                if ((pathres = ev.getPath(out string path)) == RESULT.OK)
                                {
                                    if (path.StartsWith("event:/"))
                                    {
                                        if (lastPath != null && lastBank == item.Key)
                                        {
                                            if (lastPath != path)
                                            {
                                                if (!foundLast)
                                                    continue;
                                            }
                                            else
                                            {
                                                foundLast = true;
                                                continue;
                                            }
                                        }

                                        FMODUWE.GetEventInstance(path, out EventInstance instance);
                                        instance.setVolume(1f);
                                        instance.set3DAttributes(Camera.main.transform.position.To3DAttributes());
                                        instance.start();
                                        playedSounds.Add(instance);

                                        Utils.DebugLog($"Playing {path} from bank:/{item.Key}", true);
                                        lastPath = path;
                                        lastBank = item.key;
                                        return;
                                    }
                                }
                                else
                                {
                                    Utils.DebugLog($"Error: EventDescription.getPath returned {result}", true);
                                }
                            }

                            completedBanks.Add(item.Key);
                        }
                        else
                        {
                            Utils.DebugLog($"Error: Bank.getEventList returned {result}", true);
                        }
                    }
                }
            }

            string lastPath;
            string lastBank;
            readonly List<string> completedBanks = new List<string>();
            readonly List<EventInstance> playedSounds = new List<EventInstance>();
        }
#endif
    }
}
