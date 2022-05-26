using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Default)]
    public sealed class DefaultLifepodModelInfoStub : LifepodModelInfo
    {
        protected override void prepareForIntro()
        {
        }

        public override void EndIntro(bool skipped)
        {
        }

        protected override GameObject spawnModel(out GameObject fabricator, out GameObject medicalCabinet, out GameObject radio)
        {
            fabricator = null;
            medicalCabinet = null;
            radio = null;
            return null;
        }
    }
}