using GRandomizer.Util.FabricatorPower;
using System;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.NeptuneRocket)]
    public sealed class NeptuneRocketLifepodModelInfo : LifepodModelInfo
    {
        const float ROCKET_HEIGHT = 37.75f;

        Rocket _rocket;

        Transform[] _objectsToDisableDuringIntro;

        public NeptuneRocketLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        protected override void reset()
        {
            base.reset();

            _rocket = null;
            _objectsToDisableDuringIntro = null;
        }

        protected override void prepareForIntro()
        {
            base.prepareForIntro();

            _rocket.elevatorState = Rocket.RocketElevatorStates.AtTop;
            _rocket.elevatorPosition = 1f;
            _rocket.SetElevatorPosition();

            _rocket.currentRocketStage = 5;
            _rocket.isFinished = true;
            Rocket.IsAnyRocketReady = true;

            RocketPreflightCheckManager rocketPreflightCheckManager = _rocket.GetComponent<RocketPreflightCheckManager>();
            if (rocketPreflightCheckManager.Exists())
            {
                void completeCheck(PreflightCheck check)
                {
                    rocketPreflightCheckManager.preflightChecks.Add(check);
                    rocketPreflightCheckManager.preflightCheckScreenHolder.BroadcastMessage("SetPreflightCheckComplete", check, SendMessageOptions.RequireReceiver);
                }

                completeCheck(PreflightCheck.LifeSupport);
                completeCheck(PreflightCheck.PrimaryComputer);
                completeCheck(PreflightCheck.AuxiliaryPowerUnit);
                completeCheck(PreflightCheck.CommunicationsArray);
                completeCheck(PreflightCheck.Hydraulics);

                rocketPreflightCheckManager.stageThreeAnimator.SetBool("ready", true);
                rocketPreflightCheckManager.rocketReadyDelay = true;
            }

            _objectsToDisableDuringIntro = new Transform[]
            {
                _rocket.transform.Find("Stage03/BaseRoomLadderTop"),
                _rocket.transform.Find("Stage03/EndSequenceTrigger"),
                _rocket.transform.Find("Stage03/rocketship_stage_03/Time_capsule/TimeCapsuleTrigger/Collision")
            };

            foreach (Transform disableDuringIntro in _objectsToDisableDuringIntro)
            {
                if (disableDuringIntro.Exists())
                {
                    disableDuringIntro.gameObject.SetActive(false);
                }
            }
        }

        protected override void prepareModel()
        {
            base.prepareModel();

            if (!_rocket.Exists())
            {
                _rocket = ModelObject.GetComponent<Rocket>();
            }

            if (Fabricator.Exists())
            {
                Fabricator.gameObject.AddComponent<InfiniteFabricatorPowerSource>();
            }
        }

        public override void OnLifepodPositioned()
        {
            base.OnLifepodPositioned();

            EscapePod.main.gameObject.EnsureComponent<KeepPositionAndRotation>().Initialize(false);
            _rocket.gameObject.EnsureComponent<KeepPositionAndRotation>().Initialize(false);
        }

        public override FakeParentData FakeParentData => new FakeParentData(new Vector3(0.77f, -ROCKET_HEIGHT, -1.5f), new Vector3(0f, 90f, 0f));

        // TODO: Disable "All systems are go" voiceline
        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
            GameObject rocketBase = CraftData.InstantiateFromPrefab(TechType.RocketBase);

            _rocket = rocketBase.GetComponent<Rocket>();

            _rocket.subName.SetName(LIFEPOD_NAME);

            foreach (GameObject stage in _rocket.stageObjects)
            {
                stage.SetActive(true);
            }

            GameObject fabricator = spawnStaticBuildable(TechType.Fabricator, _rocket.transform, new Vector3(-2.8f, 38.5f, 2.8f), new Vector3(25f, 135f, 0f));

            GameObject medicalCabinet = spawnStaticBuildable(TechType.MedicalCabinet, _rocket.transform, new Vector3(0f, 38.4f, -1.3f), new Vector3(355f, 180f, 0f));

            GameObject radio = spawnStaticBuildable(TechType.Radio, _rocket.transform, new Vector3(0f, 38.3f, 1.3f), new Vector3(355f, 0f, 0f));

            GameObject smallLocker = spawnStaticBuildable(TechType.SmallLocker, _rocket.transform, new Vector3(-2.8f, 38.8f, -2.8f), new Vector3(25f, 45f, 180f));

            ItemsContainer container = smallLocker.GetComponent<StorageContainer>()?.container;
            if (container != null)
            {
                foreach (TechType itemType in LootSpawner.main.GetEscapePodStorageTechTypes())
                {
                    container.AddItem(CraftData.InstantiateFromPrefab(itemType).EnsureComponent<Pickupable>().Pickup(false));
                }
            }

            onComplete?.Invoke(new LifepodModelData(rocketBase, fabricator, medicalCabinet, radio));
        }

        public override Vector3 GetOverrideLifepodPosition(Vector3 originalPos)
        {
            return base.GetOverrideLifepodPosition(originalPos).WithY(ROCKET_HEIGHT);
        }

        protected override void cleanup()
        {
            base.cleanup();

            foreach (Transform disableDuringIntro in _objectsToDisableDuringIntro)
            {
                if (disableDuringIntro.Exists())
                {
                    disableDuringIntro.gameObject.SetActive(true);
                }
            }

            KeepPositionAndRotation keepPosRot = _rocket.GetComponent<KeepPositionAndRotation>();
            if (keepPosRot.Exists())
            {
                GameObject.Destroy(keepPosRot);
            }
        }

        public override void RespawnPlayer(Player player)
        {
            player.SetPosition(_rocket.transform.TransformPoint(-2f, 38.5f, 1.75f), _rocket.transform.rotation * Quaternion.Euler(0f, 160f, 0f));
        }
    }
}
