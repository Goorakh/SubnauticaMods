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
        static readonly List<ScheduledAction> _scheduledActions = new List<ScheduledAction>();

        static GlobalObject _instance;
        public static GlobalObject Instance
        {
            get
            {
                CreateIfMissing();
                return _instance;
            }
        }

        public static void CreateIfMissing()
        {
            if (!_instance.Exists())
            {
                GameObject obj = new GameObject("GRandomizer_GlobalObject");
                _instance = obj.AddComponent<GlobalObject>();
#if DEBUG
                obj.AddComponent<DebugController>();
#endif
                GameObject.DontDestroyOnLoad(obj);
            }
        }

        public static void RunNextFrame(Action callback)
        {
            Schedule(callback, 0f);
        }

        public static void Schedule(Action callback, float waitTime)
        {
            CreateIfMissing();
            _scheduledActions.Add(new ScheduledAction(callback, Time.time + waitTime));
        }

        void Update()
        {
            for (int i = _scheduledActions.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _scheduledActions[i].ExecuteTime)
                {
                    _scheduledActions[i].Callback();
                    _scheduledActions.RemoveAt(i);
                }
            }
        }

#if DEBUG
        class DebugController : MonoBehaviour
        {
            void OnConsoleCommand_r_play(NotificationCenter.Notification n)
            {
                string arg = (string)n.data[0];
#if VERBOSE
                Utils.DebugLog(arg, true);
#endif
                FMODUWE.GetEventInstance(arg, out EventInstance instance);
                instance.setVolume(1f);
                instance.set3DAttributes(Camera.main.transform.position.To3DAttributes());
                instance.start();
                playedSounds.Add(instance);

                if (DialogueRandomizer.TryGetSubtitleKey(arg, out string subtitle))
                    DialogueRandomizer.ShowCorrectedSubtitle(subtitle);
            }

            void OnConsoleCommand_sdi(NotificationCenter.Notification n)
            {
                LootRandomizer.SetDebugIndex(int.Parse((string)n.data[0]));
            }

            void OnConsoleCommand_spas(NotificationCenter.Notification n)
            {
                Player.main.armsController.animator.SetBool((string)n.data[0], bool.Parse((string)n.data[1]));
            }

            bool banksLoading = false;
            string lastPath;
            string lastBank;
            readonly List<string> completedBanks = new List<string>();
            readonly List<EventInstance> playedSounds = new List<EventInstance>();

            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    LootRandomizer.IncreaseDebugIndex();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    LootRandomizer.DecreaseDebugIndex();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    DevConsole.RegisterConsoleCommand(this, "r_play", true, true);
                    DevConsole.RegisterConsoleCommand(this, "sdi");
                    DevConsole.RegisterConsoleCommand(this, "spas");
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

                    banksLoading = true;
                }
                else if (banksLoading && !RuntimeManager.AnyBankLoading())
                {
                    banksLoading = false;

#if VERBOSE
                    Utils.DebugLog("Banks loaded!", true);
#endif
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
                                if ((result = ev.getPath(out string path)) == RESULT.OK)
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

                                        if ((result = FMODUWE.GetEventInstance(path, out EventInstance instance)) == RESULT.OK)
                                        {
                                            instance.setVolume(1f);
                                            instance.set3DAttributes(Camera.main.transform.position.To3DAttributes());
                                            instance.start();
                                            playedSounds.Add(instance);

#if VERBOSE
                                            Utils.DebugLog($"Playing {path} from bank:/{item.Key}", true);
#endif
                                            lastPath = path;
                                            lastBank = item.key;
                                        }
                                        else
                                        {
                                            Utils.LogError($"FMODUWE.GetEventInstance returned {result}", true);
                                        }

                                        return;
                                    }
                                }
                                else
                                {
                                    Utils.LogError($"EventDescription.getPath returned {result}", true);
                                }
                            }

                            completedBanks.Add(item.Key);
                        }
                        else
                        {
                            Utils.LogError($"Bank.getEventList returned {result}", true);
                        }
                    }
                }
            }
        }
#endif
    }
}
