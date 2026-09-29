using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Visual References")]
    public GameObject paperContainer;
    public SpriteRenderer paperSprite;
    public SpriteRenderer stampAreaRenderer;
    public CanvasGroup paperUI;
    
    [Header("Paper Text References")]
    public TextMeshProUGUI namaProdukText;
    public TextMeshProUGUI produsenText;
    public TextMeshProUGUI kategoriText;
    public TextMeshProUGUI gulaText;
    public TextMeshProUGUI garamText;
    public TextMeshProUGUI lemakText;
    public TextMeshProUGUI kandunganBerbahayaText;

    [Header("Customer Visual")]
    public GameObject customerObject;
    public SpriteRenderer customerRenderer;

    [Header("Food Visual")]
    public GameObject foodObject;
    public SpriteRenderer foodRenderer;

    [Header("Audio Setup")]
    public AudioSource audioSource;
    public AudioClip paperSpawnSFX;

    void Awake()
    {
        Instance = this;
        SetPaperTextVisible(false);
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetHUDActive(false);
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

    public void StartCustomerSequence()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        paperContainer.SetActive(false);
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

            if (currentCustomer.possibleProducts.Length > 0)
            {
                currentProduct = currentCustomer.possibleProducts[Random.Range(0, currentCustomer.possibleProducts.Length)];
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

            if (currentCustomer.canTriggerEvent && Random.value <= currentCustomer.eventProbability)
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
        hasInvestigated = false;

        if (currentCustomer.possibleProducts.Length > 0 && currentProduct != null)
        {
            if (namaProdukText != null) namaProdukText.text = " " + currentProduct.namaProduk;
            if (produsenText != null) produsenText.text = currentProduct.asalProdusen;
            if (kategoriText != null) kategoriText.text = currentProduct.kategoriProduk;
            if (gulaText != null) gulaText.text = currentProduct.gula + "g";
            if (garamText != null) garamText.text = currentProduct.garam + "g";
            if (lemakText != null) lemakText.text = currentProduct.lemak + "g";
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

        paperContainer.SetActive(true);
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

        if (currentProduct.isPemalsuan)
        {
            customerRenderer.sprite = currentCustomer.spriteCemas;

            RevealFakeData();

            if (!string.IsNullOrEmpty(currentProduct.fungusFakeRevealMessage))
            {
                Flowchart.BroadcastFungusMessage(currentProduct.fungusFakeRevealMessage);
            }
        }
        else
        {
            customerRenderer.sprite = currentCustomer.spriteMarah;
            GameManager.Instance.ReduceTimer(5f);
        }
    }

    public void RevealFakeData()
    {
        if (gulaText != null) gulaText.text = currentProduct.gulaAsli + "g";
        if (garamText != null) garamText.text = currentProduct.garamAsli + "g";
        if (lemakText != null) lemakText.text = currentProduct.lemakAsli + "g";

        if (kandunganBerbahayaText != null && !string.IsNullOrEmpty(currentProduct.kandunganBerbahaya))
        {
            kandunganBerbahayaText.text = "ZAT TERLARANG: " + currentProduct.kandunganBerbahaya;
            kandunganBerbahayaText.gameObject.SetActive(true);
        }
    }

    public void EndCustomerSequence(bool isHappy)
    {
        StartCoroutine(EndSequence(isHappy));
    }

    private IEnumerator EndSequence(bool isHappy)
    {
        customerRenderer.sprite = isHappy ? currentCustomer.spriteSenang : currentCustomer.spriteMarah;
        
        yield return new WaitForSeconds(1.5f); 

        float fadeDuration = 1f;
        float timeElapsed = 0f;
        Color pColor = paperSprite.color;
        Color stampAreaColor = stampAreaRenderer != null ? stampAreaRenderer.color : Color.white;
        Color cColor = customerRenderer.color;
        Color fColor = foodRenderer != null ? foodRenderer.color : Color.white;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timeElapsed / fadeDuration);
            
            pColor.a = alpha; 
            cColor.a = alpha;
            paperSprite.color = pColor; 
            customerRenderer.color = cColor;

            if (stampAreaRenderer != null)
            {
                stampAreaColor.a = alpha;
                stampAreaRenderer.color = stampAreaColor;
            }
            
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
            GameManager.Instance.SetHUDActive(false);

        // Setelah bersih-bersih, suruh GameManager memanggil pelanggan berikutnya
        GameManager.Instance.CheckDayProgress();
    }

    private void ResetFadeColors()
    {
        Color pColor = paperSprite.color; pColor.a = 1f; paperSprite.color = pColor;
        if (stampAreaRenderer != null)
        {
            Color stampAreaColor = stampAreaRenderer.color;
            stampAreaColor.a = 1f;
            stampAreaRenderer.color = stampAreaColor;
        }
        Color cColor = customerRenderer.color; cColor.a = 1f; customerRenderer.color = cColor;
        if (foodRenderer != null)
        {
            Color fColor = foodRenderer.color; fColor.a = 1f; foodRenderer.color = fColor;
        }
        if (paperUI != null) paperUI.alpha = 1f;
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
}
