using System;
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

        public override void OnLifepodPositioned()
        {
        }

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
        }
    }
}