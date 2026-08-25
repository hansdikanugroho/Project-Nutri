using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    public int reputation = 100;
    public float customerPatience = 15f; 
    private float currentTimer;
    
    public bool isProcessing = false; 
    public bool sudahDiCap = false; 

    private ProductData activeProduct; 
    
    [Header("UI References")]
    public Slider timerSlider;

    [Header("Day Cycle System")]
    public int currentDay = 1;
    public int customersPerDay = 5; 
    private int customersServedToday = 0;
    
    [Header("Day Report UI")]
    public GameObject dayReportPanel;
    public TextMeshProUGUI dayTitleText;
    public TextMeshProUGUI reputationResultText;

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
        customersServedToday = 0;
        if (dayReportPanel != null) dayReportPanel.SetActive(false);
        
        Debug.Log("Hari ke-" + currentDay + " Dimulai!");
        CustomerManager.Instance.StartCustomerSequence();
    }

    public void OnCustomerReady(ProductData productData)
    {
        activeProduct = productData;
        sudahDiCap = false;
        isProcessing = true;
        currentTimer = customerPatience;
    }

    public void UpdateReputation(int amount)
    {
        reputation += amount;
        reputation = Mathf.Clamp(reputation, 0, maxReputation); 

        if(reputationSlider != null)
        {
            reputationSlider.value = reputation;
            UpdateBarColor(); 
        }

        if (amount < 0 && reputationBarRect != null)
        {
            StopCoroutine("ShakeReputationBar"); 
            StartCoroutine("ShakeReputationBar");
        }

        Debug.Log("Reputasi saat ini: " + reputation);
        CheckReputationZone();
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

    private void CheckReputationZone()
    {
        if (reputation <= 0 && currentSPLevel < 3)
        {
            currentSPLevel = 3;
            Debug.LogWarning("GAME OVER: Reputasi habis! Anda Dipecat.");
        }
        else if (reputation > 0 && reputation <= 33 && currentSPLevel < 2)
        {
            currentSPLevel = 2;
            Debug.LogWarning("SP 2: Reputasi masuk Bar 1 (Kritis)!");
        }
        else if (reputation > 33 && reputation <= 66 && currentSPLevel < 1)
        {
            currentSPLevel = 1;
            Debug.LogWarning("SP 1: Reputasi turun ke Bar 2!");
        }
        else if (reputation > 66)
        {
            currentSPLevel = 0; 
        }
    }

    public void ProcessDecision(NutriLevel appliedLevel)
    {
        if (sudahDiCap || !isProcessing) return; 

        sudahDiCap = true; 
        isProcessing = false; 

        bool isCorrect = (appliedLevel == activeProduct.levelSebenarnya);

        if (isCorrect) UpdateReputation(10);
        else UpdateReputation(-15);
        
        CustomerManager.Instance.EndCustomerSequence(isCorrect);
    }

    private void CustomerTimeout()
    {
        if (sudahDiCap || !isProcessing) return; 

        sudahDiCap = true;
        isProcessing = false;
        
        UpdateReputation(-20);
        
        CustomerManager.Instance.EndCustomerSequence(false);
    }

    public void CheckDayProgress()
    {
        if (currentSPLevel >= 3) return; 

        customersServedToday++;

        if (customersServedToday >= customersPerDay)
        {
            ShowDayReport();
        }
        else
        {
            CustomerManager.Instance.StartCustomerSequence();
        }
    }

    private void ShowDayReport()
    {
        dayReportPanel.SetActive(true);
        
        if (dayTitleText != null) 
            dayTitleText.text = "Laporan Akhir Hari " + currentDay;
            
        if (reputationResultText != null)
        {
            if (reputation >= 67)
                reputationResultText.text = "Kerja Bagus! Reputasi Anda: " + reputation;
            else if (reputation >= 34)
                reputationResultText.text = "Anda mendapat SP 1 hari ini. Reputasi: " + reputation;
            else
                reputationResultText.text = "Kritis! Anda mendapat SP 2. Reputasi: " + reputation;
        }

        currentDay++; 
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

    // --------------------------------------------------------
    // FUNGSI KONSEKUENSI FUNGUS (RANDOM EVENT)
    // --------------------------------------------------------
    
    // Dipanggil oleh Fungus jika pemain MENERIMA suap
    public void OnBribeAccepted()
    {
        Debug.LogWarning("Suap Diterima! Reputasi Anda turun sebagai bentuk risiko korupsi.");
        
        // Mengurangi reputasi secara langsung (akan memicu efek getar dan cek zona)
        UpdateReputation(-20); 
        
        // (Opsional) Jika Anda punya variabel uang, tambahkan di sini:
        // totalUang += 500; 

        // Lanjutkan permainan dengan memunculkan kertas di meja
        CustomerManager.Instance.ShowPaperAndStartTimer();
    }

    // Dipanggil oleh Fungus jika pemain MENOLAK suap
    public void OnBribeRejected()
    {
        Debug.Log("Suap Ditolak! Anda mempertahankan integritas pekerjaan Anda.");
        
        // Memberikan sedikit bonus reputasi karena jujur
        UpdateReputation(5); 

        // Lanjutkan permainan dengan memunculkan kertas di meja
        CustomerManager.Instance.ShowPaperAndStartTimer();
    }
}