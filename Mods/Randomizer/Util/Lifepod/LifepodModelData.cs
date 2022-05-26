using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public struct LifepodModelData
    {
        public readonly GameObject MainModel;

        public readonly Fabricator Fabricator;
        public readonly MedicalCabinet MedicalCabinet;
        public readonly Radio Radio;

        public LifepodModelData(GameObject mainModel, Fabricator fabricator, MedicalCabinet medicalCabinet, Radio radio)
        {
            MainModel = mainModel;
            Fabricator = fabricator;
            MedicalCabinet = medicalCabinet;
            Radio = radio;
        }

        public LifepodModelData(GameObject mainModel, GameObject fabricator, GameObject medicalCabinet, GameObject radio) : this(mainModel, fabricator.GetComponent<Fabricator>(), medicalCabinet.GetComponent<MedicalCabinet>(), radio.GetComponent<Radio>())
        {
        }
    }
}
