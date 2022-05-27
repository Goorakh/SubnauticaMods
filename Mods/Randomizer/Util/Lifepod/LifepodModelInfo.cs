using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public abstract class LifepodModelInfo
    {
        public readonly LifepodModelType Type;

        public LifepodModelInfo(LifepodModelType type)
        {
            Type = type;
        }

        static Dictionary<LifepodModelType, LifepodModelInfo> _modelInfoByType;
        public static void InitializeModelInfoByTypeDictionary()
        {
            if (_modelInfoByType != null)
                return;

            _modelInfoByType = (from type in TypeCollection.GetAllTypes(TypeFlags.ThisAssembly | TypeFlags.Class)
                                where !type.IsAbstract && typeof(LifepodModelInfo).IsAssignableFrom(type)
                                select new { type, ModelType = type.GetCustomAttribute<LifepodModelTypeAttribute>().Type }).ToDictionary(a => a.ModelType, a => (LifepodModelInfo)Activator.CreateInstance(a.type, new object[] { a.ModelType }));
        }

        public static LifepodModelInfo GetByType(LifepodModelType lifepodModelType)
        {
            if (_modelInfoByType.TryGetValue(lifepodModelType, out LifepodModelInfo modelInfo))
            {
                return modelInfo;
            }

            throw new KeyNotFoundException(string.Format(nameof(LifepodModelType) + ".{0} is not implemented", lifepodModelType));
        }

        public virtual bool DisableTutorial => false;

        public virtual FakeParentData FakeParentData => null;

        protected EscapePod _escapePod;

        public GameObject ModelObject { get; private set; }

        public virtual void OnLifepodPositioned()
        {
            if (ModelObject.Exists())
            {
                updateModelTransform();
            }
            else
            {
                Utils.LogError($"No model object!");
            }
        }

        protected abstract void spawnModel(Action<LifepodModelData> onComplete);

        public void Replace(EscapePod escapePod)
        {
            _escapePod = escapePod;

            spawnModel(modelData =>
            {
                ModelObject = modelData.MainModel;

                const bool DEBUG_DAMAGE = false;
                if (GameModeUtils.RequiresSurvival() || DEBUG_DAMAGE)
                {
                    if (modelData.Radio.Exists())
                    {
                        LiveMixin radioLiveMixin = modelData.Radio.liveMixin;
                        if (radioLiveMixin.Exists() && radioLiveMixin.IsFullHealth())
                        {
                            radioLiveMixin.TakeDamage(80f);
                        }
                    }

                    LiveMixin playerLiveMixin = Player.main.GetComponent<LiveMixin>();
                    if (playerLiveMixin.Exists() && playerLiveMixin.IsFullHealth())
                    {
                        playerLiveMixin.TakeDamage(20f, default(Vector3), DamageType.Normal, null);
                    }
                }

                prepareForIntro();
            });
        }

        protected virtual void prepareForIntro()
        {
            _escapePod.gameObject.DisableAllCollidersOfType<Collider>();

            _escapePod.transform.TryDisableChild("models/Life_Pod_damaged_LOD1");
            _escapePod.transform.TryDisableChild("models/Life_Pod_damaged_03/lifepod_damaged_03_geo");
            _escapePod.transform.TryDisableChild("models/Life_Pod_damaged_03/root/UISpawn");
            _escapePod.transform.TryDisableChild("ModulesRoot");
        }

        protected virtual void updateModelTransform()
        {
            FakeParentData fakeParentData = FakeParentData;
            if (fakeParentData != null)
            {
                ModelObject.transform.rotation = _escapePod.transform.rotation * fakeParentData.LocalRotation;
                ModelObject.transform.position = _escapePod.transform.TransformPoint(fakeParentData.LocalPosition);
            }
            else
            {
                ModelObject.transform.position = _escapePod.transform.position;
                ModelObject.transform.rotation = _escapePod.transform.rotation;
            }
        }

        public virtual void EndIntro(bool skipped)
        {
            if (DisableTutorial || skipped)
            {
                _escapePod.gameObject.SetActive(false);

                if (!skipped)
                {
                    GlobalObject.Schedule(IntroLifepodDirector.main.SetHudToActive, 30f);
                    GlobalObject.Schedule(IntroLifepodDirector.main.OpenPDA, 4.1f);
                    GlobalObject.Schedule(IntroLifepodDirector.main.ResetFirstUse, 8f);
                }

                cleanup();
            }
        }

        public virtual void TutorialFinished()
        {
            if (!DisableTutorial)
            {
                _escapePod.gameObject.SetActive(false);
                cleanup();
            }
        }

        protected virtual void cleanup()
        {
        }

        protected virtual GameObject spawnStaticBuildable(TechType type, Transform parent, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
        {
            GameObject obj = CraftData.InstantiateFromPrefab(type);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPos;
            obj.transform.localEulerAngles = localEuler;
            obj.transform.localScale = localScale;

            Constructable constructable = obj.GetComponent<Constructable>();
            if (constructable.Exists())
                constructable.deconstructionAllowed = false;

            if (type == TechType.MedicalCabinet)
            {
                obj.GetComponent<MedicalCabinet>().ForceSpawnMedKit();
            }

            return obj;
        }
    }
}
