using GRandomizer.Util.FabricatorPower;
using System.Collections;
using UnityEngine;
using UnityModdingUtility.Extensions;

namespace GRandomizer.Util.Lifepod
{
    public abstract class VehicleLifepodModelInfo : LifepodModelInfo
    {
        protected Vehicle _vehicle;
        protected Transform _vehicleParent;

        protected VehicleLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        protected override void reset()
        {
            base.reset();

            _vehicle = null;
            _vehicleParent = null;
        }

        protected override void prepareModel()
        {
            base.prepareModel();

            if (!_vehicle.Exists())
            {
                _vehicle = ModelObject.GetComponent<Vehicle>();
            }

            _vehicle.gameObject.AddComponent<VehicleLifepod>();

            if (!LoadedFromSaveFile)
            {
                _vehicle.subName.SetName(LIFEPOD_NAME);
            }

            if (Fabricator.Exists())
            {
                Fabricator.gameObject.AddComponent<VehicleFabricatorPowerSource>();
            }
        }

        protected override void prepareForIntro()
        {
            base.prepareForIntro();

            _vehicle.useRigidbody.isKinematic = true;
        }

        public override void EndIntro(bool skipped)
        {
            base.EndIntro(skipped);

            _vehicle.StartCoroutine(waitThenTeleportPlayerIntoVehicle());
        }

        protected virtual IEnumerator waitThenTeleportPlayerIntoVehicle()
        {
            yield return new WaitForEndOfFrame();
            _vehicle.useRigidbody.isKinematic = false;
            _vehicle.EnterVehicle(Player.main, true, false);
        }

        public override void RespawnPlayer(Player player)
        {
            _vehicle.EnterVehicle(player, true, false);
        }
    }
}