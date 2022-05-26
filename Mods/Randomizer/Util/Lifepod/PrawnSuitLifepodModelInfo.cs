using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.PrawnSuit)]
    public sealed class PrawnSuitLifepodModelInfo : VehicleLifepodModelInfo
    {
        public override bool DisableTutorial => true;

        public override FakeParentData FakeParentData => new FakeParentData(new Vector3(0.87f, 0.35f, -1.65f), new Vector3(0f, 355f, 0f));

        protected override GameObject spawnModel(out GameObject fabricator, out GameObject medicalCabinet, out GameObject radio)
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

            #region Fabricator
            fabricator = CraftData.InstantiateFromPrefab(TechType.Fabricator);
            fabricator.transform.SetParent(prawnObj.transform, false);
            fabricator.transform.localPosition = new Vector3(0f, 0.1f, -0.6f);
            fabricator.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            fabricator.transform.localScale = Vector3.one;

            Constructable fabricatorConstructable = fabricator.GetComponent<Constructable>();
            if (fabricatorConstructable.Exists())
                fabricatorConstructable.deconstructionAllowed = false;

            // Used by patches to make fabricator draw from the prawn's energy since it has no PowerRelay component
            fabricator.AddComponent<VehicleFabricator>();
            #endregion

            #region Radio
            radio = CraftData.InstantiateFromPrefab(TechType.Radio);
            radio.transform.SetParent(prawnObj.transform, false);
            radio.transform.localPosition = new Vector3(-1.25f, 1f, -0.1f);
            radio.transform.localEulerAngles = new Vector3(0f, 270f, 0f);
            radio.transform.localScale = Vector3.one;

            Constructable radioConstructable = radio.GetComponent<Constructable>();
            if (radioConstructable.Exists())
                radioConstructable.deconstructionAllowed = false;
            #endregion

            #region Medical cabinet
            medicalCabinet = CraftData.InstantiateFromPrefab(TechType.MedicalCabinet);
            medicalCabinet.transform.SetParent(prawnObj.transform, false);
            medicalCabinet.transform.localPosition = new Vector3(1.25f, 1.2f, -0.15f);
            medicalCabinet.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            medicalCabinet.transform.localScale = Vector3.one;

            Constructable medicalCabinetConstructable = medicalCabinet.GetComponent<Constructable>();
            if (medicalCabinetConstructable.Exists())
                medicalCabinetConstructable.deconstructionAllowed = false;

            medicalCabinet.GetComponent<MedicalCabinet>().ForceSpawnMedKit();
            #endregion

            return prawnObj;
        }
    }
}
