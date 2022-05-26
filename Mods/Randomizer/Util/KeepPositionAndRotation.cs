using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    public class KeepPositionAndRotation : MonoBehaviour
    {
        Vector3 _position;
        Quaternion _rotation;
        bool _localSpace;

        public void Initialize(bool localSpace)
        {
            if (_localSpace = localSpace)
            {
                _position = transform.localPosition;
                _rotation = transform.localRotation;
            }
            else
            {
                _position = transform.position;
                _rotation = transform.rotation;
            }
        }

        void FixedUpdate()
        {
            if (_localSpace)
            {
                transform.localPosition = _position;
                transform.localRotation = _rotation;
            }
            else
            {
                transform.position = _position;
                transform.rotation = _rotation;
            }
        }
    }
}
