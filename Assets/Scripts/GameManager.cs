using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
    
    [Header("UI References")]
    public Slider timerSlider;

    [Header("Day Cycle System")]
    public int currentDay = 1;
    public DayConfig[] dayConfigurations; 
    private int currentCustomersPerDay;
    private int customersServedToday = 0;
    
    [Header("Day Report UI")]
    public GameObject dayReportPanel;
    public TextMeshProUGUI dayTitleText;
    public TextMeshProUGUI reputationResultText;
    public Button nextDayButton;

    [Header("SP Popup UI")]
    public GameObject spPopupPanel;
    public TextMeshProUGUI spTitleText;
    public TextMeshProUGUI spMessageText;
    private Coroutine spPopupTimer;
    public float spPopupDuration = 5f;

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

    public void StartNewDay()
    {
        dailyReputationChange = 0;
        customersServedToday = 0;
        if (dayReportPanel != null) dayReportPanel.SetActive(false);
        if (spPopupPanel != null) spPopupPanel.SetActive(false);
        
        if (dayConfigurations != null && dayConfigurations.Length > 0)
        {
            int configIndex = Mathf.Clamp(currentDay - 1, 0, dayConfigurations.Length - 1);
            DayConfig currentConfig = dayConfigurations[configIndex];

            currentCustomersPerDay = currentConfig.targetCustomers;
            currentCustomerPatience = currentConfig.customerPatience;

            if (currentConfig.dayCustomerDatabase != null && currentConfig.dayCustomerDatabase.Length > 0)
            {
                currentCustomersPerDay = currentConfig.dayCustomerDatabase.Length;

                if (CustomerManager.Instance != null)
                    CustomerManager.Instance.SetTodayCustomers(currentConfig.dayCustomerDatabase);
            }
        }
        
        Debug.Log("Hari ke-" + currentDay + " Dimulai!");
        
        CustomerManager.Instance.GenerateDailyQueue();
        CustomerManager.Instance.StartCustomerSequence();
    }

    public void OnCustomerReady(ProductData productData)
    {
        activeProduct = productData;
        sudahDiCap = false;
        isProcessing = true;
        currentTimer = currentCustomerPatience;
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

    public void ProcessDecision(NutriLevel appliedLevel)
    {
        if (sudahDiCap || !isProcessing) return; 

        sudahDiCap = true; 
        isProcessing = false; 

        bool isCorrect = (appliedLevel == activeProduct.levelSebenarnya);

        if (isCorrect) dailyReputationChange += 10;
        else dailyReputationChange -= 15;
        
        CustomerManager.Instance.EndCustomerSequence(isCorrect);
    }

    private void CustomerTimeout()
    {
        if (sudahDiCap || !isProcessing) return; 

        sudahDiCap = true;
        isProcessing = false;
        
        dailyReputationChange -= 20;
        
        CustomerManager.Instance.EndCustomerSequence(false);
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
        Debug.Log($"[DEBUG EOD] Final Reputation: {reputation} | Current SP Level: {currentSPLevel}"); 

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
            if (spPopupPanel != null)
            {
                if (spTitleText != null)
                {
                    if (currentSPLevel == 3)
                        spTitleText.text = "GAME OVER!";
                    else if (currentSPLevel == 2)
                        spTitleText.text = "SP 2 - Kritis!";
                    else
                        spTitleText.text = "SP 1 - Peringatan!";
                }

                if (spMessageText != null)
                {
                    if (currentSPLevel == 3)
                        spMessageText.text = "Reputasi Anda habis. Anda Dipecat!";
                    else if (currentSPLevel == 2)
                        spMessageText.text = "Reputasi Anda masuk Bar 1 (Kritis). Waspada!";
                    else
                        spMessageText.text = "Reputasi Anda turun ke Bar 2. Segera perbaiki!";
                }

                spPopupPanel.transform.localScale = Vector3.one;
                spPopupPanel.transform.SetAsLastSibling();
                spPopupPanel.SetActive(true);

                if (!spPopupPanel.activeInHierarchy)
                {
                    Debug.LogWarning("[DEBUG EOD] SP popup is ACTIVE but INVISIBLE - its parent Canvas or ancestor is inactive. Check the popup's parent in the hierarchy.");
                }

                if (HasZeroScaledAncestor(spPopupPanel.transform))
                {
                    if (dayReportPanel != null)
                    {
                        spPopupPanel.transform.SetParent(dayReportPanel.transform, false);
                        spPopupPanel.transform.localScale = Vector3.one;
                        spPopupPanel.transform.localPosition = Vector3.zero;
                        Debug.LogWarning("[DEBUG EOD] SP popup had a zero-scale ancestor - reparented it onto the Day Report canvas to make it visible.");
                    }
                    else
                    {
                        Debug.LogWarning("[DEBUG EOD] SP popup has a zero-scale ancestor and dayReportPanel is NULL - cannot auto-reparent. Fix the root canvas scale in the scene.");
                    }
                }

                if (spPopupTimer != null) StopCoroutine(spPopupTimer);
                spPopupTimer = StartCoroutine(SPPopupAutoClose());
            }
            else
            {
                Debug.LogWarning("[DEBUG EOD] spPopupPanel is NULL! Assign it in the Inspector to show the SP popup.");
                ShowDayReport();
            }
        }
        else
        {
            ShowDayReport();
        }

        Debug.Log($"[DEBUG EOD] Did player get warning this day? {currentSPLevel >= 1}");
    }

    public void OnSPButtonClicked()
    {
        if (spPopupTimer != null)
        {
            StopCoroutine(spPopupTimer);
            spPopupTimer = null;
        }
        if (spPopupPanel != null) spPopupPanel.SetActive(false);
        ShowDayReport();
    }

    private IEnumerator SPPopupAutoClose()
    {
        yield return new WaitForSeconds(spPopupDuration);

        if (spPopupPanel != null) spPopupPanel.SetActive(false);
        spPopupTimer = null;
        ShowDayReport();
    }

    public void OnNextDayButtonClicked()
    {
        if (currentSPLevel >= 3)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (currentDay >= dayConfigurations.Length)
        {
            Debug.Log("All days completed! Loading Finish scene...");
            SceneManager.LoadScene("Finish");
            return;
        }

        currentDay++;
        StartNewDay();
    }

    private void ShowDayReport()
    {
        dayReportPanel.transform.localScale = Vector3.one;
        dayReportPanel.SetActive(true);
        
        if (dayTitleText != null) 
            dayTitleText.text = "Laporan Akhir Hari " + currentDay;
            
        if (reputationResultText != null)
        {
            string changeText = dailyReputationChange >= 0 ? "+" + dailyReputationChange : dailyReputationChange.ToString();
            reputationResultText.text = "Perubahan Reputasi: " + changeText + "\nReputasi Akhir: " + reputation;
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
    public CustomerData[] dayCustomerDatabase;
}
