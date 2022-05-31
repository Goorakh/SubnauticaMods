using System;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.CrashfishHome)]
    public sealed class CrashfishHomeLifepodModelInfo : LifepodModelInfo
    {
        CrashHome _home;

        public override InteriorObjectFlags ShowInteriorObjects => base.ShowInteriorObjects | InteriorObjectFlags.Storage | InteriorObjectFlags.FlyingPanel;

        public override FakeParentData FakeParentData => new FakeParentData(Vector3.zero, Vector3.zero);

        public CrashfishHomeLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        protected override void prepareForIntro()
        {
            base.prepareForIntro();

            _home.animator.SetBool("attacking", false);

            Transform flyingPanel = _escapePod.transform.Find("models/Life_Pod_damaged_03/lifepod_damaged_03_geo/life_pod_wall_panel_01_door");
            if (flyingPanel.Exists())
            {
                //flyingPanel.gameObject.RemoveComponent<SkinnedMeshRenderer>();

                GameObject crashfishObj = CraftData.InstantiateFromPrefab(TechType.Crash);
                crashfishObj.transform.SetParent(flyingPanel);
                crashfishObj.transform.localPosition = Vector3.zero;
                crashfishObj.transform.localEulerAngles = Vector3.zero;
                crashfishObj.transform.localScale = Vector3.one;

                crashfishObj.RemoveComponent<Crash>();
                crashfishObj.RemoveComponent<LargeWorldEntity>();
            }
        }

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
            GameObject crashHome = CraftData.InstantiateFromPrefab(TechType.CrashHome);
            crashHome.transform.localScale = Vector3.one * 12.5f;

            _home = crashHome.GetComponent<CrashHome>();
            crashHome.AddComponent<DisableSpawn>();

            crashHome.RemoveComponent<LargeWorldEntity>();

            onComplete?.Invoke(new LifepodModelData(crashHome, null, null, null));
        }
    }
}
