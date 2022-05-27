using FMOD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Cyclops)]
    public sealed class CyclopsLifepodModelInfo : LifepodModelInfo
    {
        SubRoot _cyclops;

        Transform _life_pod_seat_01_L;
        Transform _fire_extinguisher_01_tp;

        Rigidbody[] _rigidbodiesToEnable;
        Behaviour[] _behavioursToEnable;

        Openable _doorToUnlock;
        Transform _ladderTrigger;

        protected override void prepareForIntro()
        {
            base.prepareForIntro();

            Transform interiorModelRoot = _escapePod.transform.Find("models/Life_Pod_damaged_03/lifepod_damaged_03_geo");
            if (interiorModelRoot.Exists())
            {
                interiorModelRoot.gameObject.SetActive(true);

                const string LIFE_POD_SEAT_01_L = "life_pod_seat_01_L";
                const string FIRE_EXTINGUISHER_01_TP = "fire_extinguisher_01_tp";
                
                Transform[] enabledChildren = interiorModelRoot.DisableAllChildrenExcept(LIFE_POD_SEAT_01_L, FIRE_EXTINGUISHER_01_TP);

                _life_pod_seat_01_L = enabledChildren.FirstOrDefault(t => t.name == LIFE_POD_SEAT_01_L);
                _fire_extinguisher_01_tp = enabledChildren.FirstOrDefault(t => t.name == FIRE_EXTINGUISHER_01_TP);
            }

            Transform frontExtinguisherRoot = _cyclops.transform.Find("FireExtinguisherHolder_Fore");
            if (frontExtinguisherRoot.Exists())
            {
                FireExtinguisherHolder fireExtinguisherHolder = frontExtinguisherRoot.GetComponent<FireExtinguisherHolder>();
                if (fireExtinguisherHolder.Exists())
                {
                    fireExtinguisherHolder.hasTank = false;
                    fireExtinguisherHolder.tankObject.SetActive(false);
                }
            }

            _ladderTrigger = _cyclops.transform.Find("CyclopsMeshAnimated/LongLadderTopTrigger");
            if (_ladderTrigger.Exists())
            {
                _ladderTrigger.gameObject.SetActive(false);
            }

            _cyclops.StartCoroutine(waitThenCloseDoor());
        }

        protected override void updateModelTransform()
        {
            base.updateModelTransform();
            _cyclops.gameObject.EnsureComponent<KeepPositionAndRotation>().Initialize(false);
        }

        IEnumerator waitThenCloseDoor()
        {
            yield return new WaitForEndOfFrame();

            Transform doorTransform = _cyclops.transform.Find("CyclopsMeshAnimated/submarine_hatch_01");
            if (doorTransform.Exists())
            {
                Openable openable = doorTransform.GetComponent<Openable>();
                if (openable.Exists())
                {
                    openable.rotateTarget.transform.localRotation = openable.closedRotation;
                    openable.isOpen = false;

                    openable.SetEnabled(openable.openChecker, true);
                    openable.SetEnabled(openable.closeChecker, false);

                    openable.canLock = true;
                    openable.LockDoors();

                    _doorToUnlock = openable;
                }
            }
        }

        IEnumerator waitThenAddPowerCells()
        {
            // Wait is required since the inboundPowerSources list is not populated yet
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < _cyclops.powerRelay.inboundPowerSources.Count; i++)
            {
                IPowerInterface powerInterface = _cyclops.powerRelay.inboundPowerSources[i];
                if (powerInterface is EnergyMixin energyMixin)
                {
                    if (!energyMixin.SpawnDefault(1f))
                    {
                        Utils.LogWarning($"Could not spawn default cyclops battery at index {i}");
                    }
                }
                else
                {
                    Utils.LogWarning($"Unknown cyclops {nameof(IPowerInterface)} type '{powerInterface.GetType().Name}' at index {i}");
                }
            }
        }

        public override FakeParentData FakeParentData => new FakeParentData(new Vector3(18.6f, 1.08f, 2.8f), new Vector3(0f, 85f, 0f));

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
            LightmappedPrefabs.main.RequestScenePrefab("cyclops", prefab =>
            {
                GameObject cyclopsObj = GameObject.Instantiate(prefab, null);
                cyclopsObj.SetActive(true);

                _cyclops = cyclopsObj.GetComponent<SubRoot>();

                Player.main.SetCurrentSub(_cyclops);

                _rigidbodiesToEnable = _cyclops.gameObject.SetRigidbodiesKinematic(true);

                _behavioursToEnable = new Behaviour[]
                {
                    _cyclops.worldForces,
                    _cyclops.GetComponent<Stabilizer>()
                };

                foreach (Behaviour behaviour in _behavioursToEnable)
                {
                    behaviour.enabled = false;
                }

                _cyclops.StartCoroutine(waitThenAddPowerCells());

                GameObject fabricator = spawnStaticBuildable(TechType.Fabricator, _cyclops.transform, new Vector3(-2.0f, 0.5f, -15.8f), new Vector3(0f, 180f, 0f), Vector3.one);

                GameObject medicalCabinet = spawnStaticBuildable(TechType.MedicalCabinet, _cyclops.transform, new Vector3(2.7f, 0.7f, -21.8f), new Vector3(0f, 269.1f, 0f), Vector3.one);

                GameObject radio = spawnStaticBuildable(TechType.Radio, _cyclops.transform, new Vector3(1.9f, 0.4f, -15.8f), new Vector3(0f, 180f, 0f), Vector3.one);

                GameObject smallLocker = spawnStaticBuildable(TechType.SmallLocker, _cyclops.transform, new Vector3(-2.6f, 0.6f, -17.3f), new Vector3(0f, 89.1f, 0f), Vector3.one);

                ItemsContainer container = smallLocker.GetComponent<StorageContainer>()?.container;
                if (container != null)
                {
                    foreach (TechType itemType in LootSpawner.main.GetEscapePodStorageTechTypes())
                    {
                        container.AddItem(CraftData.InstantiateFromPrefab(itemType).EnsureComponent<Pickupable>().Pickup(false));
                    }
                }

                onComplete?.Invoke(new LifepodModelData(cyclopsObj, fabricator, medicalCabinet, radio));
            });
        }

        protected override void cleanup()
        {
            base.cleanup();

            foreach (Rigidbody rb in _rigidbodiesToEnable)
            {
#if VERBOSE
                Utils.DebugLog($"Enabling cyclops rigidbody {rb.name}");
#endif

                rb.isKinematic = false;
            }

            foreach (Behaviour behaviour in _behavioursToEnable)
            {
                behaviour.enabled = true;
            }

            if (_doorToUnlock.Exists())
            {
                _doorToUnlock.UnlockDoors();
            }

            if (_ladderTrigger.Exists())
            {
                _ladderTrigger.gameObject.SetActive(true);
            }

            KeepPositionAndRotation keepPosRot = _cyclops.GetComponent<KeepPositionAndRotation>();
            if (keepPosRot.Exists())
            {
                GameObject.Destroy(keepPosRot);
            }
        }
    }
}
