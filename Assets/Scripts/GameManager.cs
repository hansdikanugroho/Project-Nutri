using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    public int reputation = 100;
    public int dailyReputationChange = 0;
    private float currentCustomerPatience; 
    private float currentTimer;
    
    public bool isProcessing = false; 
    public bool sudahDiCap = false; 

    private ProductData activeProduct; 

    [Header("Banding & Penolakan")]
    public GameObject appealPanel;
    public Button appealReviewButton;
    public Button appealInsistButton;
    public TextMeshProUGUI appealMessageText;
    public float appealTimerMultiplier = 0.6f;
    public int penaltiBandingGanda = 25;

    private bool appealUsed = false;
    private bool isAppealWindow = false;
    private bool hasEndedThisCustomer = false;
    private bool pendingWasRejected = false;
    private bool pendingIsCorrect = false;
    private bool pendingWasTimeout = false;
    private bool hasRecordedThisCustomer = false;
    private NutriLevel pendingAppliedLevel = NutriLevel.A;
    
    [Header("Stamp Statistics")]
    public int daysPerWeek = 7;
    private readonly List<StampRecord> stampHistory = new List<StampRecord>();
    private readonly List<DayStampStats> dayStatsHistory = new List<DayStampStats>();
    private DayStampStats currentDayStats;
    private bool isWeeklyReportOpen = false;
    
    [Header("UI References")]
    public Slider timerSlider;

    [Header("Day Cycle System")]
    public int currentDay = 1;
    public DayConfig[] dayConfigurations; 
    public float CurrentEventProbabilityBonus { get; private set; }
    private int currentCustomersPerDay;
    private int customersServedToday = 0;
    
    [Header("Day Report UI")]
    public GameObject dayReportPanel;
    public TextMeshProUGUI dayTitleText;
    public TextMeshProUGUI reputationResultText;
    public TextMeshProUGUI stampStatsText;
    public Button nextDayButton;

    [Header("Weekly Report Controller")]
    public WeeklyReportPanel weeklyReportController;

    [Header("SP Popup UI")]
    public GameObject spPopupPanel;
    public TextMeshProUGUI spTitleText;
    public TextMeshProUGUI spMessageText;

    [Header("Reputation Bar System")]
    public int maxReputation = 100;
    private int currentSPLevel = 0; 
    
    [Header("Reputation UI")]
    public Slider reputationSlider;
    public Image reputationFillImage;
    public Color safeColor = Color.green;
    public Color sp1Color = Color.yellow;
    public Color sp2Color = Color.red;

    [Header("Shake Effect UI")]
    public RectTransform reputationBarRect; 
    public float shakeDuration = 0.2f;      
    public float shakeMagnitude = 10f;      
    private Vector2 originalBarPos;         

    void Awake() => Instance = this;

    void Start()
    {
        if (spPopupPanel == null) AutoResolveSPPopupRefs();

        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(OnNextDayButtonClicked);
        }

        if (reputationBarRect != null)
        {
            originalBarPos = reputationBarRect.anchoredPosition;
        }

        if(reputationSlider != null)
        {
            reputationSlider.maxValue = maxReputation;
            reputationSlider.value = reputation;
            UpdateBarColor();
        }
        StartNewDay();
    }

    private void AutoResolveSPPopupRefs()
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == "Sp Kertas")
            {
                spPopupPanel = t.gameObject;
                break;
            }
        }

        if (spPopupPanel != null)
        {
            TextMeshProUGUI[] texts = spPopupPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0) spTitleText = texts[0];
            if (texts.Length > 1) spMessageText = texts[1];
            Debug.LogWarning("[DEBUG] Auto-resolved SP popup references from scene (spPopupPanel was unassigned).");
        }
        else
        {
            Debug.LogWarning("[DEBUG] Could NOT find 'Sp Kertas' in the scene. SP popup will not appear until it is assigned in the Inspector.");
        }
    }

    private bool HasZeroScaledAncestor(Transform current)
    {
        Transform t = current.parent;
        while (t != null)
        {
            if (t.localScale == Vector3.zero) return true;
            t = t.parent;
        }
        return false;
    }

    void Update()
    {
        if (isProcessing && !sudahDiCap)
        {
            currentTimer -= Time.deltaTime;
            
            if(timerSlider != null) 
                timerSlider.value = currentTimer / currentCustomerPatience;
            
            if (currentTimer <= 0)
            {
                CustomerTimeout();
            }
        }
    }

    public void SetHUDActive(bool isActive)
    {
        if (timerSlider != null) timerSlider.gameObject.SetActive(isActive);
        if (reputationSlider != null) reputationSlider.gameObject.SetActive(isActive);
    }

    public void StartNewDay()
    {
        SetHUDActive(false);
        CurrentEventProbabilityBonus = 0f;
        dailyReputationChange = 0;
        customersServedToday = 0;
        hasRecordedThisCustomer = false;
        currentDayStats = new DayStampStats { day = currentDay };
        if (dayReportPanel != null) dayReportPanel.SetActive(false);
        if (spPopupPanel != null) spPopupPanel.SetActive(false);
        
        if (dayConfigurations != null && dayConfigurations.Length > 0)
        {
            int configIndex = Mathf.Clamp(currentDay - 1, 0, dayConfigurations.Length - 1);
            DayConfig currentConfig = dayConfigurations[configIndex];

            currentCustomersPerDay = currentConfig.targetCustomers;
            currentCustomerPatience = currentConfig.customerPatience;
            CurrentEventProbabilityBonus = currentConfig.eventProbabilityBonus;

            if (currentConfig.dayCustomerDatabase != null && currentConfig.dayCustomerDatabase.Length > 0)
            {
                currentCustomersPerDay = currentConfig.dayCustomerDatabase.Length;

                if (CustomerManager.Instance != null)
                {
                    int playableCustomerCount = CustomerManager.Instance.SetTodayCustomers(
                        currentConfig.dayCustomerDatabase,
                        currentDay);
                    if (playableCustomerCount > 0)
                    {
                        currentCustomersPerDay = playableCustomerCount;
                    }
                    else
                    {
                        Debug.LogError($"Hari {currentDay} tidak memiliki customer dengan Product SO bergambar.");
                        return;
                    }
                }
            }
        }
        
        Debug.Log("Hari ke-" + currentDay + " Dimulai!");
        
        CustomerManager.Instance.GenerateDailyQueue();
        CustomerManager.Instance.StartCustomerSequence();
    }

    public void OnCustomerReady(ProductData productData)
    {
        SetHUDActive(true);
        activeProduct = productData;
        sudahDiCap = false;
        isProcessing = true;
        appealUsed = false;
        isAppealWindow = false;
        hasEndedThisCustomer = false;
        hasRecordedThisCustomer = false;
        pendingWasTimeout = false;
        pendingWasRejected = false;
        if (appealPanel != null) appealPanel.SetActive(false);
        currentTimer = activeProduct != null && activeProduct.adaZatTerlarang ? currentCustomerPatience * 0.7f : currentCustomerPatience;
    }

    private void UpdateBarColor()
    {
        if (reputationFillImage == null) return;

        if (reputation >= 67)
            reputationFillImage.color = safeColor;
        else if (reputation >= 34)
            reputationFillImage.color = sp1Color;
        else
            reputationFillImage.color = sp2Color;
    }

    private bool MustBeRejected(ProductData product)
    {
        if (product == null) return false;
        return product.isPemalsuan
            || !string.IsNullOrEmpty(product.kandunganBerbahaya)
            || product.wajibDitolak
            || product.adaZatTerlarang;
    }

    public void ProcessDecision(NutriLevel appliedLevel)
    {
        if (sudahDiCap || !isProcessing || hasEndedThisCustomer) return;

        sudahDiCap = true;
        isProcessing = false;
        pendingAppliedLevel = appliedLevel;
        pendingWasRejected = false;
        pendingWasTimeout = false;

        bool mustReject = MustBeRejected(activeProduct);
        bool isCorrect = false;

        if (!mustReject && activeProduct != null)
        {
            NutriLevel expectedLevel = activeProduct.CalculateNutriLevel();
            isCorrect = appliedLevel == expectedLevel;
        }

        HandleVerdictResult(isCorrect, false);
    }

    public void RejectProduct()
    {
        if (sudahDiCap || !isProcessing || hasEndedThisCustomer) return;

        sudahDiCap = true;
        isProcessing = false;
        pendingWasRejected = true;
        pendingWasTimeout = false;

        bool mustReject = MustBeRejected(activeProduct);
        bool isCorrect = mustReject;

        HandleVerdictResult(isCorrect, true);
    }

    private void HandleVerdictResult(bool isCorrect, bool wasRejected)
    {
        pendingWasRejected = wasRejected;
        pendingIsCorrect = isCorrect;

        if (isCorrect)
        {
            int reward = 10;
            if (wasRejected && (activeProduct.levelZatBerbahaya == DangerLevel.Tinggi || activeProduct.adaZatTerlarang))
            {
                reward = 15;
            }
            dailyReputationChange += reward;
            FinalizeCustomer(true);
        }
        else
        {
            CustomerData currentCust = CustomerManager.Instance != null ? CustomerManager.Instance.GetCurrentCustomer() : null;
            bool canAppeal = currentCust != null && currentCust.canAppeal && !appealUsed;

            if (canAppeal)
            {
                TriggerAppeal();
            }
            else
            {
                int penalty = appealUsed ? penaltiBandingGanda : (activeProduct != null && activeProduct.penaltiTolakSalah > 0 ? activeProduct.penaltiTolakSalah : 15);
                dailyReputationChange -= penalty;
                FinalizeCustomer(false);
            }
        }
    }

    private void TriggerAppeal()
    {
        isAppealWindow = true;
        CustomerManager.Instance.ShowProtestVisual();

        if (appealPanel != null)
        {
            appealPanel.SetActive(true);
            if (appealMessageText != null)
            {
                appealMessageText.text = pendingWasRejected 
                    ? "Pelanggan memprotes penolakan berkas! Tinjau ulang atau tegaskan keputusan?" 
                    : "Pelanggan komplain nilai cap salah! Tinjau ulang atau pertahankan nilai?";
            }
        }
        else
        {
            OnAppealInsist();
        }
    }

    public void OnAppealReview()
    {
        if (!isAppealWindow) return;
        isAppealWindow = false;
        appealUsed = true;

        if (appealPanel != null) appealPanel.SetActive(false);

        sudahDiCap = false;
        isProcessing = true;
        currentTimer = currentCustomerPatience * appealTimerMultiplier;

        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.ReopenForAppeal();
        }
    }

    public void OnAppealInsist()
    {
        if (!isAppealWindow && appealUsed) return;
        isAppealWindow = false;

        if (appealPanel != null) appealPanel.SetActive(false);

        pendingIsCorrect = false;
        int penalty = appealUsed ? penaltiBandingGanda : 15;
        dailyReputationChange -= penalty;
        FinalizeCustomer(false);
    }

    private void FinalizeCustomer(bool isHappy)
    {
        if (hasEndedThisCustomer) return;
        hasEndedThisCustomer = true;
        isAppealWindow = false;
        if (appealPanel != null) appealPanel.SetActive(false);

        RecordCustomerDecision(isHappy);

        CustomerManager.Instance.EndCustomerSequence(isHappy);
    }

    private void RecordCustomerDecision(bool isHappy)
    {
        if (hasRecordedThisCustomer) return;
        hasRecordedThisCustomer = true;

        if (currentDayStats == null) currentDayStats = new DayStampStats { day = currentDay };

        currentDayStats.totalPelanggan++;

        if (pendingWasTimeout)
        {
            currentDayStats.tidakDijawab++;
        }
        else if (pendingWasRejected)
        {
            if (isHappy) currentDayStats.tolakBenar++;
            else currentDayStats.tolakSalah++;
        }
        else
        {
            if (isHappy) currentDayStats.stampBenar++;
            else currentDayStats.stampSalah++;
        }

        if (activeProduct != null)
        {
            StampRecord record = new StampRecord
            {
                day = currentDay,
                namaProduk = activeProduct.namaProduk,
                wasStamped = !pendingWasRejected && !pendingWasTimeout,
                wasRejected = pendingWasRejected,
                isTimeout = pendingWasTimeout,
                appliedLevel = pendingAppliedLevel,
                expectedLevel = activeProduct.CalculateNutriLevel(),
                isCorrect = isHappy,
                wasPemalsuan = activeProduct.isPemalsuan,
                kandunganBerbahaya = activeProduct.kandunganBerbahaya ?? string.Empty,
                levelZatBerbahaya = activeProduct.levelZatBerbahaya,
                adaZatTerlarang = activeProduct.adaZatTerlarang
            };
            stampHistory.Add(record);
        }
    }

    public void ReduceTimer(float penaltyAmount)
    {
        if (!isProcessing || sudahDiCap) return;

        currentTimer -= penaltyAmount;
        if (timerSlider != null) timerSlider.value = currentTimer / currentCustomerPatience;

        if (currentTimer <= 0)
        {
            CustomerTimeout();
        }
    }

    private void CustomerTimeout()
    {
        if (sudahDiCap || !isProcessing || hasEndedThisCustomer) return; 

        sudahDiCap = true;
        isProcessing = false;
        pendingWasTimeout = true;
        pendingWasRejected = false;
        pendingIsCorrect = false;
        
        dailyReputationChange -= 20;
        
        FinalizeCustomer(false);
    }

    public void CheckDayProgress()
    {
        if (currentSPLevel >= 3) return; 

        customersServedToday++;

        if (customersServedToday >= currentCustomersPerDay)
        {
            ProcessEndOfDay();
        }
        else
        {
            CustomerManager.Instance.StartCustomerSequence();
        }
    }

    private void ProcessEndOfDay()
    {
        reputation += dailyReputationChange;
        reputation = Mathf.Clamp(reputation, 0, maxReputation); 
        Debug.Log($"[DEBUG EOD] Reputation after this day: {reputation} | SP Level BEFORE evaluation: {currentSPLevel}"); 

        if (currentDayStats != null)
        {
            dayStatsHistory.RemoveAll(s => s.day == currentDay);
            dayStatsHistory.Add(currentDayStats);
        }

        if(reputationSlider != null)
        {
            reputationSlider.value = reputation;
            UpdateBarColor(); 
        }

        if (dailyReputationChange < 0 && reputationBarRect != null)
        {
            StopCoroutine("ShakeReputationBar"); 
            StartCoroutine("ShakeReputationBar");
        }

        if (reputation <= 0)
        {
            currentSPLevel = 3;
            Debug.Log("[DEBUG EOD] Triggered FIRED!");
        }
        else if (reputation > 33 && reputation <= 66 && currentSPLevel < 1)
        {
            currentSPLevel = 1;
            Debug.Log("[DEBUG EOD] Triggered SP 1!");
        }
        else if (reputation <= 33)
        {
            currentSPLevel = 2;
            Debug.Log("[DEBUG EOD] Triggered SP 2!");
        }
        else
        {
            currentSPLevel = 0;
            Debug.Log("[DEBUG EOD] Triggered SAFE ZONE.");
        }

        Debug.Log("Reputasi akhir hari ke-" + currentDay + ": " + reputation + " (Perubahan: " + dailyReputationChange + ")");

        if (currentSPLevel >= 1)
        {
            string title;
            if (currentSPLevel == 3)
                title = "GAME OVER!";
            else if (currentSPLevel == 2)
                title = "SP 2 - Kritis!";
            else
                title = "SP 1 - Peringatan!";

            string message;
            if (currentSPLevel == 3)
                message = "Reputasi Anda habis. Anda Dipecat!";
            else if (currentSPLevel == 2)
                message = "Reputasi Anda masuk Bar 1 (Kritis). Waspada!";
            else
                message = "Reputasi Anda turun ke Bar 2. Segera perbaiki!";

            StartCoroutine(ShowSPSequence(title, message));
        }
        else
        {
            ShowDayReport();
        }

        Debug.Log($"[DEBUG EOD] Did player get warning this day? {currentSPLevel >= 1}");
    }

    public void OnSPButtonClicked()
    {
        if (spPopupPanel != null) spPopupPanel.SetActive(false);
        ShowDayReport();
    }

    private IEnumerator ShowSPSequence(string title, string message)
    {
        if (spTitleText != null) spTitleText.text = title;
        if (spMessageText != null) spMessageText.text = message;

        if (dayReportPanel != null) dayReportPanel.SetActive(false);

        if (spPopupPanel != null)
        {
            spPopupPanel.transform.localScale = Vector3.one;
            spPopupPanel.transform.SetAsLastSibling();
            spPopupPanel.SetActive(true);

            if (!spPopupPanel.activeInHierarchy)
            {
                Debug.LogWarning("[DEBUG EOD] SP popup is ACTIVE but INVISIBLE - its parent Canvas or ancestor is inactive. Check the popup's parent in the hierarchy.");
            }

            if (HasZeroScaledAncestor(spPopupPanel.transform) && dayReportPanel != null)
            {
                spPopupPanel.transform.SetParent(dayReportPanel.transform, false);
                spPopupPanel.transform.localScale = Vector3.one;
                spPopupPanel.transform.localPosition = Vector3.zero;
                Debug.LogWarning("[DEBUG EOD] SP popup had a zero-scale ancestor - reparented it onto the Day Report canvas to make it visible.");
            }
        }
        else
        {
            Debug.LogWarning("[DEBUG EOD] spPopupPanel is NULL! Assign it in the Inspector to show the SP popup.");
        }

        yield return new WaitForSeconds(5f);

        if (spPopupPanel != null) spPopupPanel.SetActive(false);

        if (currentSPLevel >= 3)
        {
            Debug.Log("[DEBUG EOD] SP Level 3 - Game Over! Loading Finish scene...");
            SceneManager.LoadScene("Finish");
        }
        else
        {
            ShowDayReport();
        }
    }

    public void OnNextDayButtonClicked()
    {
        if (currentSPLevel >= 3)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        bool isWeekEnd = daysPerWeek > 0 && currentDay % daysPerWeek == 0;
        if (isWeekEnd && !isWeeklyReportOpen)
        {
            int weekNumber = currentDay / daysPerWeek;
            if (TryShowWeeklyReport(weekNumber))
            {
                return;
            }
        }

        ProceedToNextDay();
    }

    private bool TryShowWeeklyReport(int weekNumber)
    {
        if (weeklyReportController == null)
        {
            weeklyReportController = FindFirstObjectByType<WeeklyReportPanel>();
        }

        if (weeklyReportController == null)
        {
            weeklyReportController = AutoCreateWeeklyReportPanel();
        }

        if (weeklyReportController != null)
        {
            isWeeklyReportOpen = true;
            if (dayReportPanel != null) dayReportPanel.SetActive(false);

            int startDay = (weekNumber - 1) * daysPerWeek + 1;
            int endDay = weekNumber * daysPerWeek;

            List<StampRecord> weekRecords = stampHistory.FindAll(r => r.day >= startDay && r.day <= endDay);
            List<DayStampStats> weekDayStats = dayStatsHistory.FindAll(s => s.day >= startDay && s.day <= endDay);

            weeklyReportController.Show(weekNumber, weekRecords, weekDayStats, () =>
            {
                isWeeklyReportOpen = false;
                ProceedToNextDay();
            });
            return true;
        }

        return false;
    }

    private void ProceedToNextDay()
    {
        if (dayReportPanel != null) dayReportPanel.SetActive(false);

        if (currentDay >= dayConfigurations.Length)
        {
            Debug.Log("All days completed! Loading Finish scene...");
            SceneManager.LoadScene("Finish");
            return;
        }

        currentDay++;
        StartNewDay();
    }

    private WeeklyReportPanel AutoCreateWeeklyReportPanel()
    {
        Canvas targetCanvas = FindFirstObjectByType<Canvas>();
        if (targetCanvas == null) return null;

        GameObject panelObj = new GameObject("WeeklyReportPanel_AutoGenerated", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(WeeklyReportPanel));
        panelObj.transform.SetParent(targetCanvas.transform, false);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panelObj.GetComponent<Image>();
        img.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);

        GameObject box = new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panelObj.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.08f, 0.05f);
        boxRt.anchorMax = new Vector2(0.92f, 0.95f);
        boxRt.offsetMin = Vector2.zero; boxRt.offsetMax = Vector2.zero;
        box.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 1f);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.04f, 0.90f); titleRt.anchorMax = new Vector2(0.96f, 0.98f);
        titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.fontSize = 28; titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.fontStyle = FontStyles.Bold;

        GameObject periodObj = new GameObject("PeriodText", typeof(RectTransform), typeof(TextMeshProUGUI));
        periodObj.transform.SetParent(box.transform, false);
        RectTransform periodRt = periodObj.GetComponent<RectTransform>();
        periodRt.anchorMin = new Vector2(0.04f, 0.84f); periodRt.anchorMax = new Vector2(0.96f, 0.90f);
        periodRt.offsetMin = Vector2.zero; periodRt.offsetMax = Vector2.zero;
        TextMeshProUGUI periodTmp = periodObj.GetComponent<TextMeshProUGUI>();
        periodTmp.fontSize = 18; periodTmp.alignment = TextAlignmentOptions.Center; periodTmp.color = Color.yellow;

        GameObject summaryObj = new GameObject("SummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        summaryObj.transform.SetParent(box.transform, false);
        RectTransform sumRt = summaryObj.GetComponent<RectTransform>();
        sumRt.anchorMin = new Vector2(0.05f, 0.68f); sumRt.anchorMax = new Vector2(0.95f, 0.84f);
        sumRt.offsetMin = Vector2.zero; sumRt.offsetMax = Vector2.zero;
        TextMeshProUGUI sumTmp = summaryObj.GetComponent<TextMeshProUGUI>();
        sumTmp.fontSize = 16; sumTmp.alignment = TextAlignmentOptions.TopLeft;

        GameObject zatObj = new GameObject("ZatText", typeof(RectTransform), typeof(TextMeshProUGUI));
        zatObj.transform.SetParent(box.transform, false);
        RectTransform zatRt = zatObj.GetComponent<RectTransform>();
        zatRt.anchorMin = new Vector2(0.05f, 0.38f); zatRt.anchorMax = new Vector2(0.95f, 0.67f);
        zatRt.offsetMin = Vector2.zero; zatRt.offsetMax = Vector2.zero;
        TextMeshProUGUI zatTmp = zatObj.GetComponent<TextMeshProUGUI>();
        zatTmp.fontSize = 15; zatTmp.alignment = TextAlignmentOptions.TopLeft; zatTmp.textWrappingMode = TextWrappingModes.Normal;

        GameObject prodObj = new GameObject("ProdukText", typeof(RectTransform), typeof(TextMeshProUGUI));
        prodObj.transform.SetParent(box.transform, false);
        RectTransform prodRt = prodObj.GetComponent<RectTransform>();
        prodRt.anchorMin = new Vector2(0.05f, 0.14f); prodRt.anchorMax = new Vector2(0.95f, 0.37f);
        prodRt.offsetMin = Vector2.zero; prodRt.offsetMax = Vector2.zero;
        TextMeshProUGUI prodTmp = prodObj.GetComponent<TextMeshProUGUI>();
        prodTmp.fontSize = 14; prodTmp.alignment = TextAlignmentOptions.TopLeft; prodTmp.textWrappingMode = TextWrappingModes.Normal;

        GameObject btnObj = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(box.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.03f); btnRt.anchorMax = new Vector2(0.65f, 0.11f);
        btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;
        btnObj.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.3f, 1f);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero; btnTxtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI btnTmp = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "LANJUT"; btnTmp.fontSize = 20; btnTmp.alignment = TextAlignmentOptions.Center; btnTmp.fontStyle = FontStyles.Bold;

        WeeklyReportPanel comp = panelObj.GetComponent<WeeklyReportPanel>();
        comp.weeklyReportPanel = panelObj;
        comp.weeklyTitleText = titleTmp;
        comp.weeklyPeriodText = periodTmp;
        comp.weeklySummaryText = sumTmp;
        comp.weeklyZatBerbahayaText = zatTmp;
        comp.weeklyProdukListText = prodTmp;
        comp.weeklyContinueButton = btnObj.GetComponent<Button>();

        return comp;
    }

    private void ShowDayReport()
    {
        dayReportPanel.transform.localScale = Vector3.one;
        dayReportPanel.SetActive(true);
        
        if (dayTitleText != null) 
            dayTitleText.text = "Laporan Akhir Hari " + currentDay;
            
        string changeText = dailyReputationChange >= 0 ? "+" + dailyReputationChange : dailyReputationChange.ToString();
        string repText = "Perubahan Reputasi: " + changeText + "\nReputasi Akhir: " + reputation;

        if (currentDayStats == null)
        {
            currentDayStats = dayStatsHistory.Find(s => s.day == currentDay) ?? new DayStampStats { day = currentDay };
        }

        string statsText = $"Statistik Cap Hari Ini:\n" +
                           $"Cap Benar: <color=#55FF55>{currentDayStats.stampBenar}</color> | Cap Salah: <color=#FF5555>{currentDayStats.stampSalah}</color>\n" +
                           $"Tolak Benar: {currentDayStats.tolakBenar} | Tolak Salah: {currentDayStats.tolakSalah}\n" +
                           $"Tidak Dijawab: {currentDayStats.tidakDijawab} | Akurasi: {currentDayStats.AkurasiPersen}%";

        if (stampStatsText != null)
        {
            stampStatsText.text = statsText;
            if (reputationResultText != null) reputationResultText.text = repText;
        }
        else if (reputationResultText != null)
        {
            reputationResultText.text = repText + "\n\n" + statsText;
        }
    }

    private void OnDestroy()
    {
        if (nextDayButton != null)
        {
            nextDayButton.onClick.RemoveListener(OnNextDayButtonClicked);
        }
    }

    private IEnumerator ShakeReputationBar()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = originalBarPos.x + Random.Range(-1f, 1f) * shakeMagnitude;
            float y = originalBarPos.y + Random.Range(-1f, 1f) * shakeMagnitude;

            reputationBarRect.anchoredPosition = new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null; 
        }

        reputationBarRect.anchoredPosition = originalBarPos;
    }

    public void OnBribeAccepted()
    {
        Debug.LogWarning("Suap Diterima! Reputasi Anda turun sebagai bentuk risiko korupsi.");
        dailyReputationChange -= 20; 
        CustomerManager.Instance.ShowPaperAndStartTimer();
    }

    public void OnBribeRejected()
    {
        Debug.Log("Suap Ditolak! Anda mempertahankan integritas pekerjaan Anda.");
        dailyReputationChange += 5; 
        CustomerManager.Instance.ShowPaperAndStartTimer();
    }
}

[System.Serializable]
public class DayConfig
{
    public int targetCustomers;
    public float customerPatience;
    [Range(0f, 1f)] public float eventProbabilityBonus;
    public CustomerData[] dayCustomerDatabase;
}
