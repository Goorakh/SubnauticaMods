using System;
using System.Collections;
using UnityEngine;
using UnityModdingUtility.Extensions;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Cyclops)]
    public sealed class CyclopsLifepodModelInfo : LifepodModelInfo
    {
        SubRoot _cyclops;

        Rigidbody[] _rigidbodiesToEnable;
        Behaviour[] _behavioursToEnable;

        Openable _doorToUnlock;
        Transform _ladderTrigger;

        public override InteriorObjectFlags ShowInteriorObjects => base.ShowInteriorObjects | InteriorObjectFlags.SeatL;

        public CyclopsLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        protected override void reset()
        {
            base.reset();

            _cyclops = null;
            _rigidbodiesToEnable = null;
            _behavioursToEnable = null;
            _doorToUnlock = null;
            _ladderTrigger = null;
        }

        protected override void prepareForIntro()
        {
            base.prepareForIntro();

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

        protected override void prepareModel()
        {
            base.prepareModel();

            if (!_cyclops.Exists())
            {
                _cyclops = ModelObject.GetComponent<SubRoot>();
            }
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

                _cyclops.GetComponentInChildren<SubName>(true)?.SetName(LIFEPOD_NAME);

                Player.main.SetCurrentSub(_cyclops);

                _rigidbodiesToEnable = _cyclops.gameObject.SetRigidbodiesKinematic(true);

                _behavioursToEnable = new Behaviour[]
                {
                    _cyclops.worldForces,
                    _cyclops.GetComponent<Stabilizer>(),
                    _cyclops.GetComponentInChildren<PilotingChair>()
                };

                foreach (Behaviour behaviour in _behavioursToEnable)
                {
                    behaviour.enabled = false;
                }

                _cyclops.StartCoroutine(waitThenAddPowerCells());

                GameObject fabricator = spawnStaticBuildable(TechType.Fabricator, _cyclops.transform, new Vector3(-2.0f, 0.5f, -15.8f), new Vector3(0f, 180f, 0f));

                GameObject medicalCabinet = spawnStaticBuildable(TechType.MedicalCabinet, _cyclops.transform, new Vector3(2.7f, 0.7f, -21.8f), new Vector3(0f, 269.1f, 0f));

                GameObject radio = spawnStaticBuildable(TechType.Radio, _cyclops.transform, new Vector3(1.9f, 0.4f, -15.8f), new Vector3(0f, 180f, 0f));

                GameObject smallLocker = spawnStaticBuildable(TechType.SmallLocker, _cyclops.transform, new Vector3(-2.6f, 0.6f, -17.3f), new Vector3(0f, 89.1f, 0f));

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
