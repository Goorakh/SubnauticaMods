namespace GRandomizer.Util.FabricatorPower
{
    public class InfiniteFabricatorPowerSource : FabricatorPowerSource
    {
        public override bool ConsumeEnergy(float amount)
        {
            return true;
        }

        public override bool HasEnoughPower(float energyCost)
        {
            return true;
        }
    }
}
