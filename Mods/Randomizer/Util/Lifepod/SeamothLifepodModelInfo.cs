using System.Collections.ObjectModel;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Seamoth)]
    public sealed class SeamothLifepodModelInfo : LifepodModelInfo
    {
        // TODO: This is run every time the lifepod is initialized, meaning nothing about the seamoth gets saved. Figure out how to serialize this object in the save data to avoid creating it every time.
        protected override GameObject spawnModel(EscapePod escapePod)
        {
            GameObject seamothObj = CraftData.InstantiateFromPrefab(TechType.Seamoth);

            // Spawn default power source (powercell) with full charge
            EnergyMixin energyMixin = seamothObj.GetComponent<EnergyMixin>();
            energyMixin.SpawnDefault(1f);

            SeaMoth seamoth = seamothObj.GetComponent<SeaMoth>();
            seamoth.modules.Add(EquipmentType.SeamothModule, TechType.SeamothSolarCharge, SeaMoth.slot1ID);
            InventoryItem storageModule = seamoth.modules.Add(EquipmentType.SeamothModule, TechType.VehicleStorageModule, SeaMoth.slot3ID);

            string storageModuleSlotID = null;
            if (seamoth.modules.GetItemSlot(storageModule, ref storageModuleSlotID))
            {
                if (seamoth.TryFindSeamothStorageForSlotID(storageModuleSlotID, out ItemsContainer container))
                {
                    int width, height;

                    StorageContainer lifepodStorage = escapePod.storageContainer;
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

            seamoth.EnterVehicle(Player.main, true, false);

            #region Fabricator
            GameObject fabricatorObj = CraftData.InstantiateFromPrefab(TechType.Fabricator);
            fabricatorObj.transform.SetParent(seamothObj.transform, false);
            fabricatorObj.transform.localPosition = new Vector3(1f, -1f, 1f);
            fabricatorObj.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            fabricatorObj.transform.localScale = Vector3.one;

            Constructable fabricatorConstructable = fabricatorObj.GetComponent<Constructable>();
            if (fabricatorConstructable.Exists())
                fabricatorConstructable.deconstructionAllowed = false;

            // Used by patches to make fabricator draw from the seamoth's energy since it has no PowerRelay component
            fabricatorObj.AddComponent<SeamothFabricator>();
            #endregion

            #region Radio
            GameObject radioObj = CraftData.InstantiateFromPrefab(TechType.Radio);
            radioObj.transform.SetParent(seamothObj.transform, false);
            radioObj.transform.localPosition = new Vector3(-1f, -0.85f, 0.85f);
            radioObj.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            radioObj.transform.localScale = Vector3.one;

            Constructable radioConstructable = radioObj.GetComponent<Constructable>();
            if (radioConstructable.Exists())
                radioConstructable.deconstructionAllowed = false;
            #endregion

            #region Medical cabinet
            GameObject medicalCabinetObj = CraftData.InstantiateFromPrefab(TechType.MedicalCabinet);
            medicalCabinetObj.transform.SetParent(seamothObj.transform, false);
            medicalCabinetObj.transform.localPosition = new Vector3(0f, -1.15f, 0.5f);
            medicalCabinetObj.transform.localEulerAngles = new Vector3(87f, 0f, 0f);
            medicalCabinetObj.transform.localScale = Vector3.one;

            Constructable medicalCabinetConstructable = medicalCabinetObj.GetComponent<Constructable>();
            if (medicalCabinetConstructable.Exists())
                medicalCabinetConstructable.deconstructionAllowed = false;

            medicalCabinetObj.GetComponent<MedicalCabinet>().ForceSpawnMedKit();
            #endregion

            const bool DEBUG_DAMAGE = false;
            if (GameModeUtils.RequiresSurvival() || DEBUG_DAMAGE)
            {
                LiveMixin radioLiveMixin = radioObj.GetComponent<LiveMixin>();
                if (radioLiveMixin.Exists() && radioLiveMixin.IsFullHealth())
                {
                    radioLiveMixin.TakeDamage(80f);
                }

                LiveMixin playerLiveMixin = Player.main.GetComponent<LiveMixin>();
                if (playerLiveMixin && playerLiveMixin.IsFullHealth())
                {
                    playerLiveMixin.TakeDamage(20f, default(Vector3), DamageType.Normal, null);
                }
            }

            return seamothObj;
        }
    }
}