using UnityEngine;

namespace GRandomizer.Util.FabricatorPower
{
    public abstract class FabricatorPowerSource : MonoBehaviour
    {
        public abstract bool HasEnoughPower(float energyCost);

        public abstract bool ConsumeEnergy(float amount);
    }
}
