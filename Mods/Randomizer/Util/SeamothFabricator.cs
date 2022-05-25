using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    public class SeamothFabricator : MonoBehaviour
    {
        public SeaMoth Seamoth;

        void Awake()
        {
            Seamoth = GetComponentInParent<SeaMoth>();
        }
    }
}
