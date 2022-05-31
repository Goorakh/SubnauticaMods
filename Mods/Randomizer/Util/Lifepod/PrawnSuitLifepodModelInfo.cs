using GRandomizer.Util.FabricatorPower;
using System;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.PrawnSuit)]
    public sealed class PrawnSuitLifepodModelInfo : VehicleLifepodModelInfo
    {
        public PrawnSuitLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        public override bool DisableTutorial => true;

        public override FakeParentData FakeParentData => new FakeParentData(new Vector3(0.87f, 0.35f, -1.65f), new Vector3(0f, 355f, 0f));

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
            GameObject prawnObj = CraftData.InstantiateFromPrefab(TechType.Exosuit);

            // Spawn default power sources (powercells) with full charge
            foreach (EnergyMixin energyMixin in prawnObj.GetComponentsInChildren<EnergyMixin>())
            {
                energyMixin.SpawnDefault(1f);
            }

            Exosuit prawn = prawnObj.GetComponent<Exosuit>();
            _vehicle = prawn;

            InventoryItem storageModule = prawn.modules.Add(EquipmentType.ExosuitModule, TechType.VehicleStorageModule);

            ItemsContainer container = prawn.storageContainer.container;
            // No storage resizing, prawn storage is 6x5 (30), which is close enough to the lifepod storage size of 4x8 (32)
            foreach (TechType itemType in LootSpawner.main.GetEscapePodStorageTechTypes())
            {
                container.AddItem(CraftData.InstantiateFromPrefab(itemType).EnsureComponent<Pickupable>().Pickup(false));
            }

            GameObject fabricator = spawnStaticBuildable(TechType.Fabricator, _vehicle.transform, new Vector3(0f, 0.1f, -0.6f), new Vector3(0f, 180f, 0f));

            // Used by patches to make fabricator draw from the prawn's energy since it has no PowerRelay component
            fabricator.AddComponent<VehicleFabricatorPowerSource>();

            GameObject radio = spawnStaticBuildable(TechType.Radio, _vehicle.transform, new Vector3(-1.25f, 1f, -0.1f), new Vector3(0f, 270f, 0f));

            GameObject medicalCabinet = spawnStaticBuildable(TechType.MedicalCabinet, _vehicle.transform, new Vector3(1.25f, 1.2f, -0.15f), new Vector3(0f, 90f, 0f));

            onComplete?.Invoke(new LifepodModelData(prawnObj, fabricator, medicalCabinet, radio));
        }
    }
}
