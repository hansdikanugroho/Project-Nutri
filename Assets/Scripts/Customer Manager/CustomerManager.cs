using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Fungus; 

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance;

    private CustomerData[] todayCustomers;

    [Header("Time & Queue System")]
    public DayNightController timeController; 
    private List<CustomerData> dailyCustomerQueue = new List<CustomerData>();
    private int totalCustomersToday;

    private CustomerData currentCustomer;
    private ProductData currentProduct;
    private bool hasInvestigated = false;
    private Coroutine investigationRoutine;
    private Coroutine revealRoutine;
    private readonly List<Coroutine> revealChildren = new List<Coroutine>();
    private int revealRunning;
    private Vector2 shakeBasePosition;

    [Header("Visual References")]
    public GameObject paperContainer;
    public Image paperImage;
    public Image stampAreaImage;
    public CanvasGroup paperUI;
    public Button rejectButton;
    
    [Header("Paper Text References")]
    public TextMeshProUGUI namaProdukText;
    public TextMeshProUGUI produsenText;
    public TextMeshProUGUI kategoriText;
    public TextMeshProUGUI gulaText;
    public TextMeshProUGUI garamText;
    public TextMeshProUGUI lemakText;
    public TextMeshProUGUI kandunganBerbahayaText;

    [Header("Reveal Effect")]
    public InvestigationNoirController investigationNoir;
    public RectTransform shakeTarget;
    public AudioClip revealSFX;
    public float revealStrikeDelay = 0.35f;
    public float revealCharsPerSecond = 18f;
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 12f;

    [Header("Customer Visual")]
    public GameObject customerObject;
    public SpriteRenderer customerRenderer;

    [Header("Food Visual")]
    public GameObject foodObject;
    public SpriteRenderer foodRenderer;

    [Header("Audio Setup")]
    public AudioSource audioSource;
    public AudioClip paperSpawnSFX;

    public CustomerData GetCurrentCustomer()
    {
        return currentCustomer;
    }

    public void ShowProtestVisual()
    {
        if (customerRenderer == null || currentCustomer == null) return;
        customerRenderer.sprite = currentCustomer.spriteProtes != null ? currentCustomer.spriteProtes : currentCustomer.spriteMarah;
    }

    public void ReopenForAppeal()
    {
        hasInvestigated = false;
        if (customerRenderer != null && currentCustomer != null)
        {
            customerRenderer.sprite = currentCustomer.spriteNetral;
        }
    }

    public void OnRejectButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RejectProduct();
        }
    }

    void Awake()
    {
        Instance = this;
        SetPaperTextVisible(false);
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetHUDActive(true);
        if (foodObject != null) foodObject.SetActive(false);
    }

    public void SetTodayCustomers(CustomerData[] customersForToday)
    {
        if (customersForToday == null || customersForToday.Length == 0) return;
        todayCustomers = customersForToday;
    }

    public void GenerateDailyQueue()
    {
        dailyCustomerQueue.Clear();
        totalCustomersToday = todayCustomers.Length;

        for (int i = 0; i < totalCustomersToday; i++)
        {
            dailyCustomerQueue.Add(todayCustomers[i]);
        }
    }

    public void ResetForNewDay()
    {
        StopInvestigationRoutine();
        StopRevealRoutine();
        hasInvestigated = false;
        currentCustomer = null;
        currentProduct = null;

        ResetFadeColors();

        if (paperUI != null)
        {
            paperUI.alpha = 1f;
            paperUI.interactable = true;
            paperUI.blocksRaycasts = true;
        }

        if (paperContainer != null)
        {
            paperContainer.transform.localScale = Vector3.one;
            paperContainer.SetActive(false);
        }

        if (customerObject != null) customerObject.SetActive(false);
        if (foodObject != null) foodObject.SetActive(false);
        SetPaperTextVisible(false);
    }

    public void StartCustomerSequence()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        if (paperContainer != null) paperContainer.SetActive(false);
        if (foodObject != null) foodObject.SetActive(false);
        ResetFadeColors();

        if (dailyCustomerQueue.Count > 0)
        {
            // LOGIKA HITUNG URUTAN: 
            // Kalau database ada 2, pas orang pertama dipanggil, count jadi 0 (Pagi)
            // Pas orang kedua, count jadi 1 (Siang)
            int currentCustomerIndex = totalCustomersToday - dailyCustomerQueue.Count;

            if (timeController != null)
            {
                timeController.SetTimePhase(currentCustomerIndex);
            }

            currentCustomer = dailyCustomerQueue[0];
            dailyCustomerQueue.RemoveAt(0); 

            currentProduct = SelectEligibleProduct(currentCustomer.possibleProducts);
            if (currentProduct == null)
            {
                Debug.LogError($"Customer '{currentCustomer.name}' tidak memiliki minuman yang memenuhi syarat Nutri-Level Kemenkes 301/2026.");
                yield break;
            }

            customerRenderer.sprite = currentCustomer.spriteNetral;

            // 1. Set initial alpha to 0
            Color cColor = customerRenderer.color;
            cColor.a = 0f;
            customerRenderer.color = cColor;

            customerObject.SetActive(true);

            // 2. Smooth fade-in loop
            float fadeInDuration = 0.5f;
            float timeElapsed = 0f;
            while (timeElapsed < fadeInDuration)
            {
                timeElapsed += Time.deltaTime;
                cColor.a = Mathf.Lerp(0f, 1f, timeElapsed / fadeInDuration);
                customerRenderer.color = cColor;
                yield return null;
            }

            // 3. Ensure alpha is exactly 1 at the end
            cColor.a = 1f;
            customerRenderer.color = cColor;

            float eventProbability = currentCustomer.eventProbability;
            if (GameManager.Instance != null)
            {
                eventProbability += GameManager.Instance.CurrentEventProbabilityBonus;
            }

            if (currentCustomer.canTriggerEvent && Random.value <= Mathf.Clamp01(eventProbability))
            {
                Flowchart.BroadcastFungusMessage(currentCustomer.fungusMessageToTrigger);
                yield break; 
            }

            if (currentProduct != null && !string.IsNullOrEmpty(currentProduct.fungusIntroMessage))
            {
                Flowchart.BroadcastFungusMessage(currentProduct.fungusIntroMessage);
            }
            else
            {
                ShowPaperAndStartTimer();
            }
        }
        else
        {
            // Jika antrian habis, lapor ke GameManager untuk Akhiri Hari
            Debug.Log("Semua pelanggan di database sudah dilayani. Hari berakhir.");
            if (GameManager.Instance != null)
            {
                // Panggil fungsi show panel result lu di sini
                // GameManager.Instance.EndDay(); 
            }
        }
    }

    public void ShowPaperAndStartTimer()
    {
        StopInvestigationRoutine();
        hasInvestigated = false;
        StopRevealRoutine();

        if (currentCustomer.possibleProducts.Length > 0 && currentProduct != null)
        {
            SetSingleLineText(namaProdukText, currentProduct.namaProduk);
            SetSingleLineText(produsenText, currentProduct.asalProdusen);
            SetSingleLineText(kategoriText, currentProduct.kategoriProduk);
            if (gulaText != null) gulaText.text = FormatNutrient(currentProduct.gula, "g");
            if (garamText != null) garamText.text = FormatNutrient(currentProduct.garam, "mg");
            if (lemakText != null) lemakText.text = FormatNutrient(currentProduct.lemak, "g");
        }

        if (kandunganBerbahayaText != null)
        {
            kandunganBerbahayaText.text = "";
            kandunganBerbahayaText.gameObject.SetActive(false);
        }

        if (foodRenderer != null && currentProduct.gambarMakanan != null)
        {
            foodRenderer.sprite = currentProduct.gambarMakanan;
        }

        if (foodObject != null) foodObject.SetActive(true);

        ResetFadeColors();
        if (paperContainer != null)
        {
            paperContainer.transform.localScale = Vector3.one;
            paperContainer.SetActive(true);
        }
        SetPaperTextVisible(true);

        if (audioSource != null && paperSpawnSFX != null)
        {
            audioSource.PlayOneShot(paperSpawnSFX);
        }
        
        GameManager.Instance.OnCustomerReady(currentProduct);
    }

    public void OnInvestigateButtonClicked()
    {
        if (hasInvestigated || GameManager.Instance.sudahDiCap) return;
        hasInvestigated = true;

        investigationRoutine = StartCoroutine(InvestigateCurrentProduct());
    }

    private IEnumerator InvestigateCurrentProduct()
    {
        if (investigationNoir != null)
        {
            yield return investigationNoir.PlayInvestigation();
            investigationNoir.StopInvestigationInstant();
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.8f);
        }

        bool adaPemalsuan = currentProduct.isPemalsuan;
        bool adaZatBerbahaya = !string.IsNullOrEmpty(currentProduct.kandunganBerbahaya);

        if (adaPemalsuan || adaZatBerbahaya)
        {
            customerRenderer.sprite = currentCustomer.spriteCemas;

            RevealTrueData();

            if (!string.IsNullOrEmpty(currentProduct.fungusFakeRevealMessage))
            {
                Flowchart.BroadcastFungusMessage(currentProduct.fungusFakeRevealMessage);
            }
        }
        else
        {
            customerRenderer.sprite = currentCustomer.spriteMarah;
            if (investigationNoir != null) investigationNoir.PlayDisappointedReaction();
            GameManager.Instance.ReduceTimer(5f);
        }

        investigationRoutine = null;
    }

    public void RevealTrueData()
    {
        if (audioSource != null && revealSFX != null)
        {
            audioSource.PlayOneShot(revealSFX);
        }

        shakeBasePosition = shakeTarget != null ? shakeTarget.anchoredPosition : Vector2.zero;
        revealRoutine = StartCoroutine(RevealSequence());

        ShowHazardousSubstance();
    }

    private IEnumerator RevealSequence()
    {
        if (shakeTarget != null && shakeMagnitude > 0f)
        {
            revealRunning++;
            revealChildren.Add(StartCoroutine(TrackRevealChild(
                PaperRevealEffect.Shake(shakeTarget, shakeDuration, shakeMagnitude, shakeBasePosition))));
        }

        if (currentProduct.isPemalsuan)
        {
            SpawnRevealField(gulaText, FormatNutrient(currentProduct.gula, "g"), FormatNutrient(currentProduct.gulaAsli, "g"));
            SpawnRevealField(garamText, FormatNutrient(currentProduct.garam, "mg"), FormatNutrient(currentProduct.garamAsli, "mg"));
            SpawnRevealField(lemakText, FormatNutrient(currentProduct.lemak, "g"), FormatNutrient(currentProduct.lemakAsli, "g"));
        }

        while (revealRunning > 0) yield return null;
    }

    private void SpawnRevealField(TextMeshProUGUI label, string oldValue, string newValue)
    {
        if (label == null) return;

        revealRunning++;
        revealChildren.Add(StartCoroutine(TrackRevealChild(
            PaperRevealEffect.RevealField(label, oldValue, newValue, revealStrikeDelay, revealCharsPerSecond))));
    }

    private IEnumerator TrackRevealChild(IEnumerator routine)
    {
        yield return routine;
        revealRunning--;
    }

    private void ShowHazardousSubstance()
    {
        if (kandunganBerbahayaText == null) return;
        if (string.IsNullOrEmpty(currentProduct.kandunganBerbahaya))
        {
            kandunganBerbahayaText.text = "";
            kandunganBerbahayaText.gameObject.SetActive(false);
            return;
        }

        string line = "ZAT TERLARANG: " + currentProduct.kandunganBerbahaya;

        if (currentProduct.levelZatBerbahaya != DangerLevel.TidakAda)
        {
            string color = currentProduct.levelZatBerbahaya == DangerLevel.Tinggi ? "#FF4444" : "#FFA500";
            line += "  <color=" + color + ">" + currentProduct.levelZatBerbahaya.ToString().ToUpper() + "</color>";
        }

        if (!string.IsNullOrEmpty(currentProduct.dampakZatBerbahaya))
        {
            line += " — " + currentProduct.dampakZatBerbahaya;
        }

        kandunganBerbahayaText.text = line;
        kandunganBerbahayaText.gameObject.SetActive(true);
    }

    private void StopRevealRoutine()
    {
        bool hadReveal = revealRoutine != null || revealChildren.Count > 0;

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        for (int i = 0; i < revealChildren.Count; i++)
        {
            if (revealChildren[i] != null) StopCoroutine(revealChildren[i]);
        }

        revealChildren.Clear();
        revealRunning = 0;

        if (hadReveal && shakeTarget != null) shakeTarget.anchoredPosition = shakeBasePosition;
    }

    private void StopInvestigationRoutine()
    {
        if (investigationRoutine != null)
        {
            StopCoroutine(investigationRoutine);
            investigationRoutine = null;
        }

        if (investigationNoir != null) investigationNoir.StopInvestigationInstant();
    }

    public void EndCustomerSequence(bool isHappy)
    {
        StopInvestigationRoutine();
        StopRevealRoutine();
        StartCoroutine(EndSequence(isHappy));
    }

    private IEnumerator EndSequence(bool isHappy)
    {
        customerRenderer.sprite = isHappy ? currentCustomer.spriteSenang : currentCustomer.spriteMarah;
        
        yield return new WaitForSeconds(1.5f); 

        float fadeDuration = 1f;
        float timeElapsed = 0f;
        Color cColor = customerRenderer.color;
        Color fColor = foodRenderer != null ? foodRenderer.color : Color.white;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timeElapsed / fadeDuration);

            cColor.a = alpha;
            customerRenderer.color = cColor;

            if (foodRenderer != null)
            {
                fColor.a = alpha;
                foodRenderer.color = fColor;
            }

            if (paperUI != null) paperUI.alpha = alpha;

            yield return null;
        }

        customerObject.SetActive(false); 
        if (foodObject != null) foodObject.SetActive(false); 
        paperContainer.SetActive(false); 
        
        SetPaperTextVisible(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetHUDActive(true);

        // Setelah bersih-bersih, suruh GameManager memanggil pelanggan berikutnya
        GameManager.Instance.CheckDayProgress();
    }

    private void ResetFadeColors()
    {
        if (paperUI != null) paperUI.alpha = 1f;
        if (paperImage != null)
        {
            Color pColor = paperImage.color;
            pColor.a = 1f;
            paperImage.color = pColor;
        }
        if (stampAreaImage != null)
        {
            Color stampAreaColor = stampAreaImage.color;
            stampAreaColor.a = 1f;
            stampAreaImage.color = stampAreaColor;
        }
        if (customerRenderer != null)
        {
            Color cColor = customerRenderer.color; cColor.a = 1f; customerRenderer.color = cColor;
        }
        if (foodRenderer != null)
        {
            Color fColor = foodRenderer.color; fColor.a = 1f; foodRenderer.color = fColor;
        }
    }

    private void SetPaperTextVisible(bool isVisible)
    {
        if (namaProdukText != null) namaProdukText.gameObject.SetActive(isVisible);
        if (produsenText != null) produsenText.gameObject.SetActive(isVisible);
        if (kategoriText != null) kategoriText.gameObject.SetActive(isVisible);
        if (gulaText != null) gulaText.gameObject.SetActive(isVisible);
        if (garamText != null) garamText.gameObject.SetActive(isVisible);
        if (lemakText != null) lemakText.gameObject.SetActive(isVisible);
        if (kandunganBerbahayaText != null && !isVisible) kandunganBerbahayaText.gameObject.SetActive(false);
    }

    private static ProductData SelectEligibleProduct(ProductData[] products)
    {
        if (products == null || products.Length == 0) return null;

        List<ProductData> eligibleProducts = new List<ProductData>();
        for (int i = 0; i < products.Length; i++)
        {
            ProductData product = products[i];
            if (product != null && product.CanReceiveNutriLevel())
            {
                eligibleProducts.Add(product);
            }
        }

        return eligibleProducts.Count > 0
            ? eligibleProducts[Random.Range(0, eligibleProducts.Count)]
            : null;
    }

    private static void SetSingleLineText(TextMeshProUGUI label, string value)
    {
        if (label == null) return;

        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 26f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string FormatNutrient(float value, string unit)
    {
        string number = Mathf.Max(0f, value).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
        return number + unit;
    }
}
