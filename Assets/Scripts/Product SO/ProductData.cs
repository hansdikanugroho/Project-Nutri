using UnityEngine;

public enum NutriLevel { A, B, C, D }

[CreateAssetMenu(fileName = "NewProduct", menuName = "BPOM/Product Data")]
public class ProductData : ScriptableObject
{
    public string namaProduk;
    public Sprite gambarMakanan;
    public float gula, garam, lemak;
    public NutriLevel levelSebenarnya; // Jawaban benar yang harus ditebak player
}