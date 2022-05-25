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

        protected override GameObject spawnModel()
        {
            return null;
        }
    }
}