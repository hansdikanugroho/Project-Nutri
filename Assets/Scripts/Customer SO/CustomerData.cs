using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomer", menuName = "BPOM/Customer Data")]
public class CustomerData : ScriptableObject
{
    public string namaCustomer;
    
    [Header("Ekspresi Karakter")]
    public Sprite spriteNetral;
    public Sprite spriteSenang;
    public Sprite spriteMarah;
    
    [Header("Daftar Produk Bawaan")]
    public ProductData[] possibleProducts; // SO Produk dimasukkan ke sini

    [Header("Fungus Random Event")]
    public bool canTriggerEvent;
    [Range(0f, 1f)] public float eventProbability = 0.2f; // 0.2 = 20% peluang
    public string fungusMessageToTrigger = "EventSuap_01"; // Pesan penanda untuk Fungus
}