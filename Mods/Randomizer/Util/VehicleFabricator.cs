using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    public class VehicleFabricator : MonoBehaviour
    {
        public Vehicle Vehicle;

        void Awake()
        {
            Vehicle = GetComponentInParent<Vehicle>();
        }
    }
}
