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

    private readonly List<CustomerData> todayCustomers = new List<CustomerData>();

    [Header("Time & Queue System")]
    public DayNightController timeController; 

    [Header("Difficulty Progression")]
    [Tooltip("Jumlah hari per tier: 7 = minggu 1 Easy, minggu 2 Normal, minggu 3 Hard.")]
    [Min(1)] public int daysPerDifficultyTier = 7;
    private List<CustomerData> dailyCustomerQueue = new List<CustomerData>();
    private int totalCustomersToday;

    private CustomerData currentCustomer;
    private ProductData currentProduct;
    private bool hasInvestigated = false;
    private Coroutine investigationRoutine;
    private Coroutine revealRoutine;
    private Coroutine spawnRoutine;
    private Coroutine eventWatchdogRoutine;
    private readonly List<Coroutine> revealChildren = new List<Coroutine>();
    private int revealRunning;
    private Vector2 shakeBasePosition;
    private int customerSequenceVersion;
    private bool paperShownForCurrentCustomer;
    private bool waitingForFungusEvent;
    private string pendingFungusMessage = string.Empty;

    [Header("Event Safety")]
    [Tooltip("Batas waktu nyata untuk menunggu callback event Fungus. Paper muncul otomatis jika event tidak pernah selesai.")]
    [Min(1f)] public float eventResponseTimeout = 15f;

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

    [Header("Hazard Notification")]
    public GameObject hazardNotificationPanel;
    public TextMeshProUGUI hazardNotificationTitle;
    public TextMeshProUGUI hazardNotificationMessage;

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
        customerRenderer.sprite = currentCustomer.GetProtestSprite();
    }

    public void ReopenForAppeal()
    {
        hasInvestigated = false;
        if (customerRenderer != null && currentCustomer != null)
        {
            customerRenderer.sprite = currentCustomer.GetNeutralSprite();
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
        HideHazardNotification();
    }

    void Start()
    {
        if (foodObject != null) foodObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopPendingEventWatchdog();
        StopSpawnRoutine();
        StopInvestigationRoutine();
        StopRevealRoutine();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int SetTodayCustomers(CustomerData[] customersForToday, int currentDay)
    {
        todayCustomers.Clear();
        if (customersForToday == null) return 0;

        CustomerDifficulty expectedDifficulty = GetDifficultyForDay(currentDay);

        for (int i = 0; i < customersForToday.Length; i++)
        {
            CustomerData customer = customersForToday[i];
            if (customer == null)
            {
                Debug.LogWarning($"Slot customer ke-{i + 1} kosong dan dilewati.");
                continue;
            }

            if (customer.difficulty != expectedDifficulty)
            {
                Debug.LogWarning(
                    $"Customer '{customer.name}' bertier {customer.difficulty}, " +
                    $"tetapi hari {currentDay} membutuhkan tier {expectedDifficulty}. Customer dilewati.");
                continue;
            }

            if (!customer.HasPlayableProduct())
            {
                Debug.LogWarning($"Customer '{customer.name}' dilewati karena belum memiliki Product SO dengan sprite.");
                continue;
            }

            todayCustomers.Add(customer);
        }

        return todayCustomers.Count;
    }

    public CustomerDifficulty GetDifficultyForDay(int day)
    {
        int tierLength = Mathf.Max(1, daysPerDifficultyTier);
        int tierIndex = Mathf.Clamp((Mathf.Max(1, day) - 1) / tierLength, 0, 2);
        return (CustomerDifficulty)tierIndex;
    }

    public void GenerateDailyQueue()
    {
        dailyCustomerQueue.Clear();
        totalCustomersToday = todayCustomers.Count;

        for (int i = 0; i < totalCustomersToday; i++)
        {
            dailyCustomerQueue.Add(todayCustomers[i]);
        }

        // Fisher-Yates: urutan pelanggan tidak selalu sama walau pool hari sama.
        for (int i = dailyCustomerQueue.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            CustomerData temp = dailyCustomerQueue[i];
            dailyCustomerQueue[i] = dailyCustomerQueue[swapIndex];
            dailyCustomerQueue[swapIndex] = temp;
        }
    }

    public void ResetForNewDay()
    {
        HideHazardNotification();
        StopPendingEventWatchdog();
        StopSpawnRoutine();
        StopInvestigationRoutine();
        StopRevealRoutine();
        hasInvestigated = false;
        paperShownForCurrentCustomer = false;
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
        bool isCurrentPaperActive = paperShownForCurrentCustomer
            && GameManager.Instance != null
            && GameManager.Instance.isProcessing;

        if (spawnRoutine != null || waitingForFungusEvent || isCurrentPaperActive)
        {
            Debug.LogWarning("[CustomerManager] Permintaan spawn diabaikan karena customer sebelumnya masih aktif.");
            return;
        }

        StopPendingEventWatchdog();
        customerSequenceVersion++;
        paperShownForCurrentCustomer = false;
        spawnRoutine = StartCoroutine(SpawnSequence(customerSequenceVersion));
    }

    private IEnumerator SpawnSequence(int sequenceVersion)
    {
        // Pastikan field spawnRoutine sudah menerima handle coroutine sebelum jalur recovery dapat memulai sequence berikutnya.
        yield return null;

        HideHazardNotification();
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

            currentProduct = SelectPlayableProduct(currentCustomer.possibleProducts);
            if (currentProduct == null)
            {
                Debug.LogError($"Customer '{currentCustomer.name}' tidak memiliki Product SO dengan sprite yang siap dimainkan.");
                spawnRoutine = null;
                RecoverFromBrokenCustomer();
                yield break;
            }

            if (customerRenderer == null || customerObject == null)
            {
                Debug.LogError("[CustomerManager] Referensi customerObject/customerRenderer belum diisi. Customer dilewati agar game tidak stuck.");
                spawnRoutine = null;
                RecoverFromBrokenCustomer();
                yield break;
            }

            customerRenderer.sprite = currentCustomer.GetNeutralSprite();

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
                timeElapsed += Time.unscaledDeltaTime;
                cColor.a = Mathf.Lerp(0f, 1f, timeElapsed / fadeInDuration);
                customerRenderer.color = cColor;
                yield return null;
            }

            if (sequenceVersion != customerSequenceVersion)
            {
                spawnRoutine = null;
                yield break;
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
                spawnRoutine = null;
                if (!TryStartFungusEvent(currentCustomer.fungusMessageToTrigger, sequenceVersion, "random customer event"))
                {
                    ShowPaperAndStartTimer();
                }
                yield break;
            }

            if (currentProduct != null && !string.IsNullOrEmpty(currentProduct.fungusIntroMessage))
            {
                spawnRoutine = null;
                if (!TryStartFungusEvent(currentProduct.fungusIntroMessage, sequenceVersion, "product intro"))
                {
                    ShowPaperAndStartTimer();
                }
            }
            else
            {
                spawnRoutine = null;
                ShowPaperAndStartTimer();
            }
        }
        else
        {
            spawnRoutine = null;
            Debug.LogError("[CustomerManager] Antrean customer kosong lebih awal. Slot dianggap selesai agar hari tidak stuck.");
            if (GameManager.Instance != null) GameManager.Instance.CheckDayProgress();
        }
    }

    private bool TryStartFungusEvent(string message, int sequenceVersion, string source)
    {
        string safeMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        if (string.IsNullOrEmpty(safeMessage))
        {
            Debug.LogWarning($"[CustomerManager] {source} tidak memiliki nama pesan Fungus. Melanjutkan langsung ke paper.");
            return false;
        }

        MessageReceived[] receivers = FindObjectsByType<MessageReceived>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        bool hasValidReceiver = false;

        for (int i = 0; i < receivers.Length; i++)
        {
            MessageReceived receiver = receivers[i];
            if (receiver == null || !receiver.isActiveAndEnabled) continue;
            if (!string.Equals(receiver.GetSummary(), safeMessage, System.StringComparison.Ordinal)) continue;

            Block block = receiver.ParentBlock;
            Flowchart flowchart = block != null ? block.GetFlowchart() : null;
            if (block != null
                && !block.IsExecuting()
                && block._EventHandler == receiver
                && flowchart != null
                && flowchart.isActiveAndEnabled)
            {
                hasValidReceiver = true;
                break;
            }
        }

        if (!hasValidReceiver)
        {
            Debug.LogWarning(
                $"[CustomerManager] Pesan Fungus '{safeMessage}' dari {source} tidak mempunyai MessageReceived aktif yang cocok. " +
                "Paper ditampilkan sebagai fallback agar game tidak stuck.");
            return false;
        }

        StopPendingEventWatchdog();
        waitingForFungusEvent = true;
        pendingFungusMessage = safeMessage;
        eventWatchdogRoutine = StartCoroutine(WatchFungusEvent(sequenceVersion, safeMessage));
        Flowchart.BroadcastFungusMessage(safeMessage);
        return true;
    }

    private IEnumerator WatchFungusEvent(int sequenceVersion, string message)
    {
        float timeout = Mathf.Max(1f, eventResponseTimeout);
        float deadline = Time.realtimeSinceStartup + timeout;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (sequenceVersion != customerSequenceVersion || paperShownForCurrentCustomer || !waitingForFungusEvent)
            {
                eventWatchdogRoutine = null;
                yield break;
            }
            yield return null;
        }

        eventWatchdogRoutine = null;
        if (sequenceVersion != customerSequenceVersion || paperShownForCurrentCustomer) yield break;

        Debug.LogError(
            $"[CustomerManager] Event Fungus '{message}' tidak menyelesaikan callback dalam {timeout:0.#} detik. " +
            "Event dihentikan dan paper ditampilkan otomatis.");
        StopTimedOutFungusEvent(message);
        ShowPaperAndStartTimer();
    }

    private static void StopTimedOutFungusEvent(string message)
    {
        MessageReceived[] receivers = FindObjectsByType<MessageReceived>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < receivers.Length; i++)
        {
            MessageReceived receiver = receivers[i];
            if (receiver == null || !string.Equals(receiver.GetSummary(), message, System.StringComparison.Ordinal)) continue;

            Block block = receiver.ParentBlock;
            if (block != null && block.IsExecuting()) block.Stop();
        }

        if (MenuDialog.ActiveMenuDialog != null)
        {
            MenuDialog.ActiveMenuDialog.Clear();
            MenuDialog.ActiveMenuDialog.SetActive(false);
        }

        if (SayDialog.ActiveSayDialog != null)
        {
            SayDialog.ActiveSayDialog.SetActive(false);
        }
    }

    private void StopPendingEventWatchdog()
    {
        if (eventWatchdogRoutine != null)
        {
            StopCoroutine(eventWatchdogRoutine);
            eventWatchdogRoutine = null;
        }

        waitingForFungusEvent = false;
        pendingFungusMessage = string.Empty;
    }

    private void StopSpawnRoutine()
    {
        if (spawnRoutine == null) return;
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private void RecoverFromBrokenCustomer()
    {
        currentCustomer = null;
        currentProduct = null;
        if (customerObject != null) customerObject.SetActive(false);
        if (foodObject != null) foodObject.SetActive(false);
        if (paperContainer != null) paperContainer.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckDayProgress();
        }
    }

    public void ShowPaperAndStartTimer()
    {
        if (paperShownForCurrentCustomer)
        {
            Debug.LogWarning("[CustomerManager] Permintaan menampilkan paper kedua diabaikan.");
            return;
        }

        if (currentCustomer == null || currentProduct == null)
        {
            Debug.LogError("[CustomerManager] Paper tidak dapat ditampilkan karena customer/product aktif tidak valid.");
            return;
        }

        if (paperContainer == null)
        {
            Debug.LogError("[CustomerManager] Referensi paperContainer kosong. Customer dilewati agar game tidak stuck.");
            RecoverFromBrokenCustomer();
            return;
        }

        paperShownForCurrentCustomer = true;
        StopPendingEventWatchdog();
        HideHazardNotification();
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

        if (!paperContainer.activeInHierarchy)
        {
            Debug.LogError("[CustomerManager] Paper aktif tetapi parent/Canvas-nya nonaktif. Customer dilewati agar game tidak stuck.");
            paperShownForCurrentCustomer = false;
            RecoverFromBrokenCustomer();
            return;
        }

        if (audioSource != null && paperSpawnSFX != null)
        {
            audioSource.PlayOneShot(paperSpawnSFX);
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCustomerReady(currentProduct);
        }
        else
        {
            Debug.LogError("[CustomerManager] GameManager tidak tersedia saat paper ditampilkan.");
        }
    }

    public bool TryCompletePendingEvent()
    {
        if (!waitingForFungusEvent || paperShownForCurrentCustomer)
        {
            Debug.LogWarning($"[CustomerManager] Callback event '{pendingFungusMessage}' diabaikan karena event sudah selesai atau tidak lagi aktif.");
            return false;
        }

        ShowPaperAndStartTimer();
        return paperShownForCurrentCustomer;
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
            customerRenderer.sprite = currentCustomer.GetWorriedSprite();

            RevealTrueData();

            if (!string.IsNullOrEmpty(currentProduct.fungusFakeRevealMessage))
            {
                Flowchart.BroadcastFungusMessage(currentProduct.fungusFakeRevealMessage);
            }
        }
        else
        {
            customerRenderer.sprite = currentCustomer.GetAngrySprite();
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
        if (kandunganBerbahayaText != null)
        {
            kandunganBerbahayaText.text = "";
            kandunganBerbahayaText.gameObject.SetActive(false);
        }

        if (currentProduct == null || string.IsNullOrWhiteSpace(currentProduct.kandunganBerbahaya))
        {
            HideHazardNotification();
            return;
        }

        if (hazardNotificationTitle != null)
        {
            hazardNotificationTitle.text = "BAHAN BERBAHAYA TERDETEKSI!";
        }

        string message = currentProduct.kandunganBerbahaya.Trim();

        if (currentProduct.levelZatBerbahaya != DangerLevel.TidakAda)
        {
            string color = currentProduct.levelZatBerbahaya == DangerLevel.Tinggi ? "#991B1B" : "#A85D00";
            message += "  <color=" + color + "><b>" + currentProduct.levelZatBerbahaya.ToString().ToUpper() + "</b></color>";
        }

        if (!string.IsNullOrWhiteSpace(currentProduct.dampakZatBerbahaya))
        {
            message += "\nRisiko: " + currentProduct.dampakZatBerbahaya.Trim();
        }

        if (hazardNotificationMessage != null) hazardNotificationMessage.text = message;
        if (hazardNotificationPanel != null) hazardNotificationPanel.SetActive(true);
    }

    private void HideHazardNotification()
    {
        if (hazardNotificationMessage != null) hazardNotificationMessage.text = "";
        if (hazardNotificationPanel != null) hazardNotificationPanel.SetActive(false);
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

    public void EndCustomerSequence(bool isHappy, bool wasRejected = false, bool isCorrect = false)
    {
        HideHazardNotification();
        StopInvestigationRoutine();
        StopRevealRoutine();
        StartCoroutine(EndSequence(isHappy, wasRejected, isCorrect));
    }

    private IEnumerator EndSequence(bool isHappy, bool wasRejected, bool isCorrect)
    {
        if (wasRejected)
        {
            customerRenderer.sprite = currentCustomer.GetAngrySprite();
        }
        else
        {
            customerRenderer.sprite = isHappy ? currentCustomer.GetHappySprite() : currentCustomer.GetAngrySprite();
        }
        
        yield return new WaitForSecondsRealtime(1.5f);

        float fadeDuration = 1f;
        float timeElapsed = 0f;
        Color cColor = customerRenderer.color;
        Color fColor = foodRenderer != null ? foodRenderer.color : Color.white;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.unscaledDeltaTime;
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

        if (customerObject != null) customerObject.SetActive(false);
        if (foodObject != null) foodObject.SetActive(false); 
        if (paperContainer != null) paperContainer.SetActive(false);
        
        SetPaperTextVisible(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetHUDActive(true);

        // Setelah bersih-bersih, suruh GameManager memanggil pelanggan berikutnya
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckDayProgress();
        }
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

    private static ProductData SelectPlayableProduct(ProductData[] products)
    {
        if (products == null || products.Length == 0) return null;

        ProductData selected = null;
        int playableCount = 0;

        for (int i = 0; i < products.Length; i++)
        {
            ProductData product = products[i];
            if (product == null || product.gambarMakanan == null) continue;

            playableCount++;
            if (Random.Range(0, playableCount) == 0) selected = product;
        }

        return selected;
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
