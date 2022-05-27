namespace GRandomizer.Util.FabricatorPower
{
    public class VehicleFabricatorPowerSource : FabricatorPowerSource
    {
        public Vehicle Vehicle;

        void Awake()
        {
            Vehicle = GetComponentInParent<Vehicle>();
        }

        public override bool ConsumeEnergy(float amount)
        {
            return Vehicle.ConsumeEnergy(amount);
        }

        public override bool HasEnoughPower(float energyCost)
        {
            return Vehicle.HasEnoughEnergy(energyCost);
        }
    }
}
