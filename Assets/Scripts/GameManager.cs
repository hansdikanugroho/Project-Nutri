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
    private float customerPatience = 15f; 
    private float currentTimer;
    
    public bool isProcessing = false; 
    public bool sudahDiCap = false; 

    private ProductData activeProduct; 
    
    [Header("UI References")]
    public Slider timerSlider;

    [Header("Day Cycle System")]
    public int currentDay = 1;
    public DayConfig[] dayConfigurations; 
    private int customersPerDay = 5; 
    private int customersServedToday = 0;
    
    [Header("Day Report UI")]
    public GameObject dayReportPanel;
    public TextMeshProUGUI dayTitleText;
    public TextMeshProUGUI reputationResultText;

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

    void Update()
    {
        if (isProcessing && !sudahDiCap)
        {
            currentTimer -= Time.deltaTime;
            
            if(timerSlider != null) 
                timerSlider.value = currentTimer / customerPatience;
            
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
        
        // Loading konfigurasi hari ini; kalau index di luar array, pakai index terakhir (endless mode)
        if (dayConfigurations != null && dayConfigurations.Length > 0)
        {
            int dayIndex = Mathf.Clamp(currentDay - 1, 0, dayConfigurations.Length - 1);
            DayConfig activeConfig = dayConfigurations[dayIndex];

            customersPerDay = activeConfig.targetCustomers;
            customerPatience = activeConfig.customerPatience;

            if (activeConfig.dayCustomerDatabase != null && activeConfig.dayCustomerDatabase.Length > 0)
            {
                if (CustomerManager.Instance != null)
                    CustomerManager.Instance.SetCustomerDatabase(activeConfig.dayCustomerDatabase);
            }
        }
        
        Debug.Log("Hari ke-" + currentDay + " Dimulai!");
        
        // Generate antrian harian dulu baru panggil pelanggan
        CustomerManager.Instance.GenerateDailyQueue();
        CustomerManager.Instance.StartCustomerSequence();
    }

    public void OnCustomerReady(ProductData productData)
    {
        activeProduct = productData;
        sudahDiCap = false;
        isProcessing = true;
        currentTimer = customerPatience;
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

        if (customersServedToday >= customersPerDay)
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
            currentSPLevel = 3;
        else if (reputation <= 33)
            currentSPLevel = 2;
        else if (reputation <= 66)
            currentSPLevel = 1;
        else
            currentSPLevel = 0;

        Debug.Log("Reputasi akhir hari ke-" + currentDay + ": " + reputation + " (Perubahan: " + dailyReputationChange + ")");

        if (currentSPLevel >= 1)
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

            if (spPopupPanel != null) spPopupPanel.SetActive(true);
        }
        else
        {
            ShowDayReport();
        }
    }

    public void OnSPButtonClicked()
    {
        if (spPopupPanel != null) spPopupPanel.SetActive(false);
        ShowDayReport();
    }

    public void OnNextDayButtonClicked()
    {
        if (currentSPLevel >= 3)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        currentDay++;
        StartNewDay();
    }

    private void ShowDayReport()
    {
        dayReportPanel.SetActive(true);
        
        if (dayTitleText != null) 
            dayTitleText.text = "Laporan Akhir Hari " + currentDay;
            
        if (reputationResultText != null)
        {
            string changeText = dailyReputationChange >= 0 ? "+" + dailyReputationChange : dailyReputationChange.ToString();
            reputationResultText.text = "Perubahan Reputasi: " + changeText + "\nReputasi Akhir: " + reputation;
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