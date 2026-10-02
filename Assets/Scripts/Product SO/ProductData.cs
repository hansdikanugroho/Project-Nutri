using UnityEngine;

public enum NutriLevel { A, B, C, D }
public enum DangerLevel { TidakAda, Sedang, Tinggi }
public enum SweetenerType { TidakAda, Alami, Buatan }
public enum NutriProductType { MakananPadat, MinumanSiapSaji }

[CreateAssetMenu(fileName = "NewProduct", menuName = "BPOM/Product Data")]
public class ProductData : ScriptableObject
{
    [Header("Product Information")]
    public string namaProduk;
    public string asalProdusen;
    public string kategoriProduk;
    public Sprite gambarMakanan;

    [Header("Kelayakan Nutri-Level Kemenkes 301/2026")]
    public NutriProductType jenisProdukNutri;
    [Tooltip("Aktifkan untuk minuman yang secara alami tidak mengandung gula, garam, dan lemak sehingga tidak mencantumkan Nutri-Level.")]
    public bool alamiTanpaGGL;

    [Header("Kandungan minuman per 100 mL")]
    [Tooltip("Monosakarida dan disakarida yang diperhitungkan, tidak termasuk laktosa.")]
    [Min(0f)] public float gula;
    [Tooltip("Kandungan garam dalam miligram per 100 mL.")]
    [Min(0f)] public float garam;
    [Tooltip("Kandungan lemak jenuh dalam gram per 100 mL, bukan total lemak.")]
    [Min(0f)] public float lemak;
    public SweetenerType jenisPemanis;
    [Tooltip("Hanya digunakan untuk minuman yang memenuhi syarat Nutri-Level.")]
    public NutriLevel levelSebenarnya;

    [Header("Data Pemalsuan (Investigasi)")]
    public bool isPemalsuan;
    public string kandunganBerbahaya;
    public string dampakZatBerbahaya;
    public DangerLevel levelZatBerbahaya;

    [Header("Zat Terlarang & Penolakan")]
    public bool wajibDitolak;
    public bool adaZatTerlarang;
    public int penaltiTolakSalah = 15;

    [Header("Data Asli (Jika Pemalsuan)")]
    [Min(0f)] public float gulaAsli;
    [Min(0f)] public float garamAsli;
    [Min(0f)] public float lemakAsli;
    public SweetenerType jenisPemanisAsli;
    public NutriLevel levelAsli;

    [Header("Fungus Dialogues")]
    public string fungusIntroMessage;
    public string fungusFakeRevealMessage;

    public NutriLevel CalculateNutriLevel()
    {
        return CalculateNutriLevel(gula, garam, lemak, jenisPemanis);
    }

    public bool CanReceiveNutriLevel()
    {
        return jenisProdukNutri == NutriProductType.MinumanSiapSaji && !alamiTanpaGGL;
    }

    public bool TryCalculateNutriLevel(out NutriLevel level)
    {
        level = CalculateNutriLevel();
        return CanReceiveNutriLevel();
    }

    public NutriLevel CalculateTrueNutriLevel()
    {
        return CalculateNutriLevel(gulaAsli, garamAsli, lemakAsli, jenisPemanisAsli);
    }

    public static NutriLevel CalculateNutriLevel(
        float sugarGrams,
        float saltMilligrams,
        float saturatedFatGrams,
        SweetenerType sweetenerType = SweetenerType.TidakAda)
    {
        int worstLevel = Mathf.Max(
            GetSugarLevel(Mathf.Max(0f, sugarGrams)),
            GetSaltLevel(Mathf.Max(0f, saltMilligrams)),
            GetSaturatedFatLevel(Mathf.Max(0f, saturatedFatGrams)));

        // Kepmenkes 301/2026: level A tidak boleh memakai pemanis tambahan,
        // sedangkan level B hanya dapat memakai pemanis alami.
        if (sweetenerType == SweetenerType.Alami)
        {
            worstLevel = Mathf.Max(worstLevel, (int)NutriLevel.B);
        }
        else if (sweetenerType == SweetenerType.Buatan)
        {
            worstLevel = Mathf.Max(worstLevel, (int)NutriLevel.C);
        }

        return (NutriLevel)worstLevel;
    }

    private static int GetSugarLevel(float value)
    {
        if (value <= 1f) return (int)NutriLevel.A;
        if (value <= 5f) return (int)NutriLevel.B;
        if (value <= 10f) return (int)NutriLevel.C;
        return (int)NutriLevel.D;
    }

    private static int GetSaltLevel(float value)
    {
        if (value <= 5f) return (int)NutriLevel.A;
        if (value <= 120f) return (int)NutriLevel.B;
        if (value <= 500f) return (int)NutriLevel.C;
        return (int)NutriLevel.D;
    }

    private static int GetSaturatedFatLevel(float value)
    {
        if (value <= 0.7f) return (int)NutriLevel.A;
        if (value <= 1.2f) return (int)NutriLevel.B;
        if (value <= 2.8f) return (int)NutriLevel.C;
        return (int)NutriLevel.D;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (CanReceiveNutriLevel())
        {
            levelSebenarnya = CalculateNutriLevel();
        }

        if (isPemalsuan && jenisProdukNutri == NutriProductType.MinumanSiapSaji)
        {
            levelAsli = CalculateTrueNutriLevel();
        }
    }
#endif
}
