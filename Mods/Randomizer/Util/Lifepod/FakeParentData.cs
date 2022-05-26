using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public class FakeParentData
    {
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;

        public FakeParentData(Vector3 localPosition, Vector3 localEulerAngles)
        {
            LocalPosition = localPosition;
            LocalRotation = Quaternion.Euler(localEulerAngles);
        }
    }
}
