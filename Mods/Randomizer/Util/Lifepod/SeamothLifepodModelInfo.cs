using System;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Seamoth)]
    public sealed class SeamothLifepodModelInfo : VehicleLifepodModelInfo
    {
        public override bool DisableTutorial => true;

        public override FakeParentData FakeParentData => new FakeParentData(new Vector3(0.8f, 1.5f, -1.3f), new Vector3(0f, 355f, 0f));

        // TODO: This is run every time the lifepod is initialized, meaning nothing about the seamoth gets saved. Figure out how to serialize this object in the save data to avoid creating it every time.
        protected override GameObject spawnModel(out GameObject fabricator, out GameObject medicalCabinet, out GameObject radio)
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
                    int width, height;

                    StorageContainer lifepodStorage = _escapePod.storageContainer;
                    if (lifepodStorage.Exists())
                    {
                        width = lifepodStorage.width;
                        height = lifepodStorage.height;
                    }
                    else
                    {
                        width = 4;
                        height = 8;
                    }

                    container.Resize(width, height);

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

            #region Fabricator
            fabricator = CraftData.InstantiateFromPrefab(TechType.Fabricator);
            fabricator.transform.SetParent(seamothObj.transform, false);
            fabricator.transform.localPosition = new Vector3(1f, -1f, 1f);
            fabricator.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            fabricator.transform.localScale = Vector3.one;

            Constructable fabricatorConstructable = fabricator.GetComponent<Constructable>();
            if (fabricatorConstructable.Exists())
                fabricatorConstructable.deconstructionAllowed = false;

            // Used by patches to make fabricator draw from the seamoth's energy since it has no PowerRelay component
            fabricator.AddComponent<VehicleFabricator>();
            #endregion

            #region Radio
            radio = CraftData.InstantiateFromPrefab(TechType.Radio);
            radio.transform.SetParent(seamothObj.transform, false);
            radio.transform.localPosition = new Vector3(-1f, -0.85f, 0.85f);
            radio.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            radio.transform.localScale = Vector3.one;

            Constructable radioConstructable = radio.GetComponent<Constructable>();
            if (radioConstructable.Exists())
                radioConstructable.deconstructionAllowed = false;
            #endregion

            #region Medical cabinet
            medicalCabinet = CraftData.InstantiateFromPrefab(TechType.MedicalCabinet);
            medicalCabinet.transform.SetParent(seamothObj.transform, false);
            medicalCabinet.transform.localPosition = new Vector3(0f, -1.15f, 0.5f);
            medicalCabinet.transform.localEulerAngles = new Vector3(87f, 0f, 0f);
            medicalCabinet.transform.localScale = Vector3.one;

            Constructable medicalCabinetConstructable = medicalCabinet.GetComponent<Constructable>();
            if (medicalCabinetConstructable.Exists())
                medicalCabinetConstructable.deconstructionAllowed = false;

            medicalCabinet.GetComponent<MedicalCabinet>().ForceSpawnMedKit();
            #endregion

            return seamothObj;
        }
    }
}