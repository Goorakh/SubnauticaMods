﻿using GRandomizer.Util.FabricatorPower;
using System;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Seamoth)]
    public sealed class SeamothLifepodModelInfo : VehicleLifepodModelInfo
    {
        public SeamothLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        public override bool DisableTutorial => true;

        public override FakeParentData FakeParentData => new FakeParentData(new Vector3(0.8f, 1.5f, -1.3f), new Vector3(0f, 355f, 0f));

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
            GameObject seamothObj = CraftData.InstantiateFromPrefab(TechType.Seamoth);

            // Spawn default power source (powercell) with full charge
            EnergyMixin energyMixin = seamothObj.GetComponent<EnergyMixin>();
            energyMixin.SpawnDefault(1f);

            SeaMoth seamoth = seamothObj.GetComponent<SeaMoth>();
            _vehicle = seamoth;

            seamoth.modules.Add(EquipmentType.SeamothModule, TechType.SeamothSolarCharge, SeaMoth.slot1ID);
            InventoryItem storageModule = seamoth.modules.Add(EquipmentType.SeamothModule, TechType.VehicleStorageModule, SeaMoth.slot3ID);

            string storageModuleSlotID = null;
            if (seamoth.modules.GetItemSlot(storageModule, ref storageModuleSlotID))
            {
                if (seamoth.TryFindSeamothStorageForSlotID(storageModuleSlotID, out ItemsContainer container))
                {
                    foreach (TechType itemType in LootSpawner.main.GetEscapePodStorageTechTypes())
                    {
                        container.AddItem(CraftData.InstantiateFromPrefab(itemType).EnsureComponent<Pickupable>().Pickup(false));
                    }
                }
            }
            else
            {
                Utils.LogWarning("Seamoth storage module is not in any slot");
            }

            GameObject fabricator = spawnStaticBuildable(TechType.Fabricator, _vehicle.transform, new Vector3(1f, -1f, 1f), new Vector3(90f, 0f, 0f));

            GameObject radio = spawnStaticBuildable(TechType.Radio, _vehicle.transform, new Vector3(-1f, -0.85f, 0.85f), new Vector3(90f, 0f, 0f));

            GameObject medicalCabinet = spawnStaticBuildable(TechType.MedicalCabinet, _vehicle.transform, new Vector3(0f, -1.15f, 0.5f), new Vector3(87f, 0f, 0f));

            onComplete?.Invoke(new LifepodModelData(seamothObj, fabricator, medicalCabinet, radio));
        }
    }
}