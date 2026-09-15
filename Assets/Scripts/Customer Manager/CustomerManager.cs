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

    [Header("Visual References")]
    public GameObject paperContainer;
    public SpriteRenderer paperSprite;
    public SpriteRenderer stampAreaRenderer;
    public CanvasGroup paperUI;
    
    [Header("Paper Text References")]
    public TextMeshProUGUI namaProdukText;
    public TextMeshProUGUI gulaText;
    public TextMeshProUGUI garamText;
    public TextMeshProUGUI lemakText;

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
            
            customerRenderer.sprite = currentCustomer.spriteNetral;
            customerObject.SetActive(true); 

            yield return new WaitForSeconds(3f); 

            if (currentCustomer.canTriggerEvent && Random.value <= currentCustomer.eventProbability)
            {
                Flowchart.BroadcastFungusMessage(currentCustomer.fungusMessageToTrigger);
                yield break; 
            }

            ShowPaperAndStartTimer();
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
        if (currentCustomer.possibleProducts.Length > 0)
        {
            currentProduct = currentCustomer.possibleProducts[Random.Range(0, currentCustomer.possibleProducts.Length)];
            
            namaProdukText.text = " " + currentProduct.namaProduk;
            gulaText.text = "Gula: " + currentProduct.gula + "g";
            garamText.text = "Garam: " + currentProduct.garam + "g";
            lemakText.text = "Lemak: " + currentProduct.lemak + "g";
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
        if (gulaText != null) gulaText.gameObject.SetActive(isVisible);
        if (garamText != null) garamText.gameObject.SetActive(isVisible);
        if (lemakText != null) lemakText.gameObject.SetActive(isVisible);
    }
}
