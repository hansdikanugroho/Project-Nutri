using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomer", menuName = "BPOM/Customer Data")]
public class CustomerData : ScriptableObject
{
    public string namaCustomer;
    
    [Header("Ekspresi Karakter")]
    public Sprite spriteNetral;
    public Sprite spriteSenang;
    public Sprite spriteMarah;
    public Sprite spriteCemas;
    public Sprite spriteProtes;
    
    [Header("Daftar Produk Bawaan")]
    public ProductData[] possibleProducts;

    [Header("Fungus Random Event")]
    public bool canTriggerEvent;
    [Range(0f, 1f)] public float eventProbability = 0.2f;
    public string fungusMessageToTrigger = "EventSuap_01";

    [Header("Fungus Appeal & Protes")]
    public bool canAppeal = true;
    public string fungusAppealMessage = "EventBanding_01";
}