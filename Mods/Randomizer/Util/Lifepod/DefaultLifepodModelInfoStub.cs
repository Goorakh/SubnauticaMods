using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    [LifepodModelType(LifepodModelType.Default)]
    public sealed class DefaultLifepodModelInfoStub : LifepodModelInfo
    {
        protected override GameObject spawnModel(EscapePod escapePod)
        {
            return null;
        }
    }
}