using System.Collections;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public abstract class VehicleLifepodModelInfo : LifepodModelInfo
    {
        protected Vehicle _vehicle;
        protected Transform _vehicleParent;

        protected VehicleLifepodModelInfo(LifepodModelType type) : base(type)
        {
        }

        protected override void prepareForIntro()
        {
            base.prepareForIntro();

            _vehicle.useRigidbody.isKinematic = true;

            _vehicle.gameObject.AddComponent<VehicleLifepod>();
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