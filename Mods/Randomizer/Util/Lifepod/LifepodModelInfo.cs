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
        static Dictionary<LifepodModelType, LifepodModelInfo> _modelInfoByType;
        public static void InitializeModelInfoByTypeDictionary()
        {
            if (_modelInfoByType != null)
                return;

            _modelInfoByType = (from type in TypeCollection.GetAllTypes(TypeFlags.ThisAssembly | TypeFlags.Class)
                                where !type.IsAbstract && typeof(LifepodModelInfo).IsAssignableFrom(type)
                                select new { type, ModelType = type.GetCustomAttribute<LifepodModelTypeAttribute>().Type }).ToDictionary(a => a.ModelType, a => (LifepodModelInfo)Activator.CreateInstance(a.type));
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

        protected abstract GameObject spawnModel(out GameObject fabricator, out GameObject medicalCabinet, out GameObject radio);

        public void Replace(EscapePod escapePod)
        {
            _escapePod = escapePod;

            ModelObject = spawnModel(out _, out _, out GameObject radio);

            const bool DEBUG_DAMAGE = false;
            if (GameModeUtils.RequiresSurvival() || DEBUG_DAMAGE)
            {
                LiveMixin radioLiveMixin = radio.GetComponent<LiveMixin>();
                if (radioLiveMixin.Exists() && radioLiveMixin.IsFullHealth())
                {
                    radioLiveMixin.TakeDamage(80f);
                }

                LiveMixin playerLiveMixin = Player.main.GetComponent<LiveMixin>();
                if (playerLiveMixin && playerLiveMixin.IsFullHealth())
                {
                    playerLiveMixin.TakeDamage(20f, default(Vector3), DamageType.Normal, null);
                }
            }

            prepareForIntro();
        }

        protected virtual void prepareForIntro()
        {
            _escapePod.gameObject.DisableAllCollidersOfType<Collider>();

            _escapePod.transform.TryDisableChild("models/Life_Pod_damaged_03/lifepod_damaged_03_geo");
            _escapePod.transform.TryDisableChild("models/Life_Pod_damaged_03/root/UISpawn");
            _escapePod.transform.TryDisableChild("ModulesRoot");
        }

        protected virtual void updateModelTransform()
        {
            FakeParentData fakeParentData = FakeParentData;
            if (fakeParentData != null)
            {
                ModelObject.transform.rotation = (_escapePod.transform.localToWorldMatrix * Matrix4x4.Rotate(fakeParentData.LocalRotation)).rotation;
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
            _escapePod.gameObject.SetActive(false);

            if (!skipped && DisableTutorial)
            {
                GlobalObject.Schedule(IntroLifepodDirector.main.SetHudToActive, 30f);
                GlobalObject.Schedule(IntroLifepodDirector.main.OpenPDA, 4.1f);
                GlobalObject.Schedule(IntroLifepodDirector.main.ResetFirstUse, 8f);
            }
        }
    }
}
