using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fungus;

public class TutorialFlowController : MonoBehaviour
{
    public static TutorialFlowController Instance;

    [Header("Fungus References")]
    public Flowchart flowchart;

    [Header("Tutorial Customers")]
    public CustomerData customer1;
    public CustomerData customer2;

    [Header("Runtime Tutorial Data")]
    public bool createFallbackData = true;
    public bool useRealStampController = true;

    [Header("Scene Flow")]
    [SerializeField] private string mainGameSceneName = "Main Dev_2Nopal";

    private ProductData fallbackSafeProduct;
    private ProductData fallbackDangerousProduct;
    private CustomerData fallbackCustomer1;
    private CustomerData fallbackCustomer2;
    private int currentStep;
    private bool firstDecisionSent;
    private bool investigateSent;
    private bool rejectSent;
    public bool IsProcessing { get; private set; }
    public bool HasDecided { get; private set; }
    public TutorialCustomerManager customerManager;

    void Awake()
    {
        Instance = this;
        if (createFallbackData) CreateFallbackDataIfMissing();
    }

    void Start()
    {
        if (!useRealStampController) StartCoroutine(WaitForFungusThenSpawn());
    }

    private IEnumerator WaitForFungusThenSpawn()
    {
        yield return new WaitForSeconds(1f);
        SpawnCustomer1();
    }

    public void SpawnCustomer1()
    {
        currentStep = 1;
        firstDecisionSent = false;
        CustomerData data = customer1 != null ? customer1 : fallbackCustomer1;
        SpawnCustomer(data);
    }

    public void SpawnCustomer2()
    {
        currentStep = 2;
        investigateSent = false;
        rejectSent = false;
        CustomerData data = customer2 != null ? customer2 : fallbackCustomer2;
        SpawnCustomer(data);
    }

    private void SpawnCustomer(CustomerData data)
    {
        if (customerManager == null || data == null) return;
        IsProcessing = false;
        HasDecided = false;
        customerManager.SpawnCustomer(data, currentStep == 2);
    }

    public void OnCustomerReady(ProductData product)
    {
        IsProcessing = product != null;
        HasDecided = false;
    }

    public void ProcessDecision(NutriLevel level)
    {
        if (!IsProcessing || HasDecided) return;
        HasDecided = true;
        IsProcessing = false;
        NotifyDecisionMade(false, level);
        if (customerManager != null)
            customerManager.EndCustomerSequence(true);
    }

    public void RejectProduct()
    {
        if (!IsProcessing || HasDecided) return;
        HasDecided = true;
        IsProcessing = false;
        NotifyDecisionMade(true, NutriLevel.D);
        if (customerManager != null)
            customerManager.EndCustomerSequence(true);
    }

    public void NotifyDecisionMade(bool wasRejected, NutriLevel level)
    {
        if (currentStep == 1 && !wasRejected && !firstDecisionSent)
        {
            firstDecisionSent = true;
            flowchart?.SendFungusMessage("Tutorial_Customer2_Intro");
        }
        else if (currentStep == 2 && wasRejected && !rejectSent)
        {
            rejectSent = true;
            flowchart?.SendFungusMessage("Tutorial_Finished");
        }
    }

    public void NotifyInvestigateClicked()
    {
        if (currentStep != 2 || investigateSent) return;
        investigateSent = true;
        flowchart?.SendFungusMessage("Tutorial_Reject_Instruction");
    }

    public void FinishTutorialAndLoadMainDev()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainGameSceneName))
        {
            Debug.LogError($"Scene tujuan tutorial '{mainGameSceneName}' belum terdaftar di Build Profiles.", this);
            return;
        }

        SceneManager.LoadScene(mainGameSceneName);
    }

    private void CreateFallbackDataIfMissing()
    {
        fallbackSafeProduct = CreateProduct("Produk Tutorial Aman", NutriLevel.A, false);
        fallbackDangerousProduct = CreateProduct("Produk Tutorial Bermasalah", NutriLevel.C, true);
        fallbackCustomer1 = CreateCustomer("Pelanggan Tutorial 1", fallbackSafeProduct);
        fallbackCustomer2 = CreateCustomer("Pelanggan Tutorial 2", fallbackDangerousProduct);
    }

    private ProductData CreateProduct(string productName, NutriLevel level, bool dangerous)
    {
        ProductData product = ScriptableObject.CreateInstance<ProductData>();
        product.namaProduk = productName;
        product.asalProdusen = "Produsen Tutorial";
        product.kategoriProduk = "Minuman Tutorial";
        product.jenisProdukNutri = NutriProductType.MinumanSiapSaji;
        product.levelSebenarnya = level;
        product.gula = level == NutriLevel.A ? 0.5f : 7f;
        product.garam = level == NutriLevel.A ? 2f : 250f;
        product.lemak = level == NutriLevel.A ? 0.3f : 2f;
        product.isPemalsuan = dangerous;
        product.wajibDitolak = dangerous;
        product.adaZatTerlarang = dangerous;
        product.kandunganBerbahaya = dangerous ? "Zat berbahaya tutorial" : string.Empty;
        product.dampakZatBerbahaya = dangerous ? "Membahayakan kesehatan" : string.Empty;
        product.levelZatBerbahaya = dangerous ? DangerLevel.Tinggi : DangerLevel.TidakAda;
        return product;
    }

    private CustomerData CreateCustomer(string customerName, ProductData product)
    {
        CustomerData customer = ScriptableObject.CreateInstance<CustomerData>();
        customer.namaCustomer = customerName;
        customer.possibleProducts = new[] { product };
        customer.canTriggerEvent = false;
        return customer;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        DestroyRuntimeData(fallbackCustomer1);
        DestroyRuntimeData(fallbackCustomer2);
        DestroyRuntimeData(fallbackSafeProduct);
        DestroyRuntimeData(fallbackDangerousProduct);
    }

    private void DestroyRuntimeData(Object target)
    {
        if (target != null) Destroy(target);
    }
}
