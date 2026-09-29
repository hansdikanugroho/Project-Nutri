using UnityEngine;

public enum NutriLevel { A, B, C, D }

[CreateAssetMenu(fileName = "NewProduct", menuName = "BPOM/Product Data")]
public class ProductData : ScriptableObject
{
    [Header("Product Information")]
    public string namaProduk;
    public string asalProdusen;
    public string kategoriProduk;
    public Sprite gambarMakanan;
    public float gula, garam, lemak;
    public NutriLevel levelSebenarnya; // Jawaban benar yang harus ditebak player

    [Header("Data Pemalsuan (Investigasi)")]
    public bool isPemalsuan;
    public string kandunganBerbahaya; // e.g., "Formalin", "Boraks", "Rhodamin B", "Pemanis Buatan Berlebih"

    [Header("Data Asli (Jika Pemalsuan)")]
    public int gulaAsli;
    public int garamAsli;
    public int lemakAsli;
    public NutriLevel levelAsli;

    [Header("Fungus Dialogues")]
    public string fungusIntroMessage;
    public string fungusFakeRevealMessage;
}