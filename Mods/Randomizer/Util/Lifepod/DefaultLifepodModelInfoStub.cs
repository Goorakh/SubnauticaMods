using System;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Default)]
    public sealed class DefaultLifepodModelInfoStub : LifepodModelInfo
    {
        public DefaultLifepodModelInfoStub(LifepodModelType type) : base(type)
        {
        }

        protected override void spawnModel(Action<LifepodModelData> onComplete)
        {
        }
    }
}