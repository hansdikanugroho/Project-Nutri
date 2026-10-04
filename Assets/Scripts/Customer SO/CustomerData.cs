using UnityEngine;

public enum CustomerDifficulty { Easy, Normal, Hard }

[CreateAssetMenu(fileName = "NewCustomer", menuName = "BPOM/Customer Data")]
public class CustomerData : ScriptableObject
{
    public string namaCustomer;

    [Header("Difficulty Tier")]
    public CustomerDifficulty difficulty;
    
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

    public Sprite GetNeutralSprite()
    {
        return spriteNetral != null ? spriteNetral : GetFirstAvailableSprite();
    }

    public Sprite GetHappySprite()
    {
        return spriteSenang != null ? spriteSenang : GetNeutralSprite();
    }

    public Sprite GetAngrySprite()
    {
        return spriteMarah != null ? spriteMarah : GetNeutralSprite();
    }

    public Sprite GetWorriedSprite()
    {
        return spriteCemas != null ? spriteCemas : GetNeutralSprite();
    }

    public Sprite GetProtestSprite()
    {
        return spriteProtes != null ? spriteProtes : GetAngrySprite();
    }

    public bool HasPlayableProduct()
    {
        if (possibleProducts == null) return false;

        for (int i = 0; i < possibleProducts.Length; i++)
        {
            if (possibleProducts[i] != null && possibleProducts[i].gambarMakanan != null) return true;
        }

        return false;
    }

    private Sprite GetFirstAvailableSprite()
    {
        if (spriteNetral != null) return spriteNetral;
        if (spriteSenang != null) return spriteSenang;
        if (spriteMarah != null) return spriteMarah;
        if (spriteCemas != null) return spriteCemas;
        return spriteProtes;
    }
}
