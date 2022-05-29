using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public struct LifepodModelData
    {
        public readonly GameObject MainModel;

        public readonly Fabricator Fabricator;
        public readonly MedicalCabinet MedicalCabinet;
        public readonly Radio Radio;

        public LifepodModelData(GameObject mainModel, GameObject fabricator, GameObject medicalCabinet, GameObject radio)
        {
            MainModel = mainModel;
            Fabricator = fabricator?.GetComponent<Fabricator>();
            MedicalCabinet = medicalCabinet?.GetComponent<MedicalCabinet>();
            Radio = radio?.GetComponent<Radio>();
        }
    }
}
