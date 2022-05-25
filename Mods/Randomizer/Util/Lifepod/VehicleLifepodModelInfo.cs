using System.Collections;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public abstract class VehicleLifepodModelInfo : LifepodModelInfo
    {
        protected Vehicle _vehicle;
        protected Transform _vehicleParent;

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
    }
}