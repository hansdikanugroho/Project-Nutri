using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fungus; 

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance;

    [Header("Database")]
    public CustomerData[] customerDatabase;

    [Header("Time & Queue System")]
    public DayNightController timeController; 
    private List<CustomerData> dailyCustomerQueue = new List<CustomerData>();
    private int totalCustomersToday;

    private CustomerData currentCustomer;
    private ProductData currentProduct;

    [Header("Visual References")]
    public GameObject paperContainer;
    public SpriteRenderer paperSprite;
    public CanvasGroup paperUI;
    
    [Header("Paper Text References")]
    public TextMeshProUGUI namaProdukText;
    public TextMeshProUGUI gulaText;
    public TextMeshProUGUI garamText;
    public TextMeshProUGUI lemakText;

    [Header("Customer Visual")]
    public GameObject customerObject;
    public SpriteRenderer customerRenderer;

    void Awake() => Instance = this;

    // Pastikan fungsi ini dipanggil sekali saat HARI BARU dimulai (misal dari GameManager.StartNewDay)
    public void GenerateDailyQueue()
    {
        dailyCustomerQueue.Clear();
        totalCustomersToday = customerDatabase.Length; // Langsung baca dari jumlah Database di Inspector

        // Memasukkan semua customer ke dalam antrian hari ini
        for (int i = 0; i < totalCustomersToday; i++)
        {
            dailyCustomerQueue.Add(customerDatabase[i]);
        }
    }

    public void StartCustomerSequence()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        paperContainer.SetActive(false);
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
            
            namaProdukText.text = "Pengaju: " + currentProduct.namaProduk;
            gulaText.text = "Gula: " + currentProduct.gula + "g";
            garamText.text = "Garam: " + currentProduct.garam + "g";
            lemakText.text = "Lemak: " + currentProduct.lemak + "g";
        }

        paperContainer.SetActive(true);
        namaProdukText.gameObject.SetActive(true);
        gulaText.gameObject.SetActive(true);
        garamText.gameObject.SetActive(true);
        lemakText.gameObject.SetActive(true);
        
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
        Color cColor = customerRenderer.color;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timeElapsed / fadeDuration);
            
            pColor.a = alpha; 
            cColor.a = alpha;
            paperSprite.color = pColor; 
            customerRenderer.color = cColor;
            
            if (paperUI != null) paperUI.alpha = alpha;
            
            yield return null;
        }

        customerObject.SetActive(false); 
        paperContainer.SetActive(false); 
        
        namaProdukText.gameObject.SetActive(false);
        gulaText.gameObject.SetActive(false);
        garamText.gameObject.SetActive(false);
        lemakText.gameObject.SetActive(false);

        // Setelah bersih-bersih, suruh GameManager memanggil pelanggan berikutnya
        GameManager.Instance.CheckDayProgress();
    }

    private void ResetFadeColors()
    {
        Color pColor = paperSprite.color; pColor.a = 1f; paperSprite.color = pColor;
        Color cColor = customerRenderer.color; cColor.a = 1f; customerRenderer.color = cColor;
        if (paperUI != null) paperUI.alpha = 1f;
    }
}