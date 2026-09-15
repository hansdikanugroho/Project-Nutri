using UnityEngine;

namespace ProductSO1
{
    public enum NutriLevel { A, B, C, D }

    [CreateAssetMenu(fileName = "NewLegacyProduct", menuName = "BPOM/Legacy Product Data")]
    public class ProductData : ScriptableObject
    {
        public string namaProduk;
        public Sprite gambarMakanan;
        public float gula, garam, lemak;
        public NutriLevel levelSebenarnya;
    }
}
