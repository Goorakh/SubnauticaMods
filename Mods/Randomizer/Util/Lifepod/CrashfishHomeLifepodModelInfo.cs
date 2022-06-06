using System;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    //[LifepodModelType(LifepodModelType.CrashfishHome)]
    public sealed class CrashfishHomeLifepodModelInfo : LifepodModelInfo
    {
        CrashHome _home;

        public override InteriorObjectFlags ShowInteriorObjects => base.ShowInteriorObjects | InteriorObjectFlags.Storage | InteriorObjectFlags.FlyingPanel;

        public override FakeParentData FakeParentData => new FakeParentData(Vector3.zero, Vector3.zero);

        public CrashfishHomeLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
            GameObject crashHome = CraftData.InstantiateFromPrefab(TechType.CrashHome);
            crashHome.transform.localScale = Vector3.one * 12.5f;

            crashHome.AddBoxCollider(Vector3.zero, new Vector3(0.5f, 0.015f, 0.5f));

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(crashHome.transform);
            cube.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            cube.transform.localScale = Vector3.one / 12.5f;

            _home = crashHome.GetComponent<CrashHome>();
            _home.prevClosed = false;

            crashHome.AddComponent<DisableSpawn>();

            crashHome.RemoveComponent<LargeWorldEntity>();

            onComplete?.Invoke(new LifepodModelData(crashHome, null, null, null));
        }
    }
}
