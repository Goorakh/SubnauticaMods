using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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

        public GameObject ModelObject { get; private set; }

        public virtual void SetPosition(Vector3 position)
        {
            if (ModelObject.Exists())
            {
                ModelObject.transform.position = position;
            }
            else
            {
                Utils.LogError($"No model object!");
            }
        }

        protected abstract GameObject spawnModel(EscapePod escapePod);

        public void Replace(EscapePod escapePod)
        {
            if ((ModelObject = spawnModel(escapePod)) != null)
            {
                escapePod.gameObject.DisableAllCollidersOfType<Collider>();
            }
        }
    }
}
