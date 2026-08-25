using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Fungus; // Wajib untuk memanggil Flowchart

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance;

    [Header("Database")]
    public CustomerData[] customerDatabase;

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

    public void StartCustomerSequence()
    {
        StartCoroutine(SpawnSequence());
    }

    // --------------------------------------------------------
    // FASE 1: PELANGGAN DATANG & CEK EVENT FUNGUS
    // --------------------------------------------------------
    private IEnumerator SpawnSequence()
    {
        // Bersihkan meja dari sisa kertas sebelumnya
        paperContainer.SetActive(false);
        ResetFadeColors();

        if (customerDatabase.Length > 0)
        {
            // 1. Gacha Pelanggan
            currentCustomer = customerDatabase[Random.Range(0, customerDatabase.Length)];
            
            customerRenderer.sprite = currentCustomer.spriteNetral;
            customerObject.SetActive(true); // Pastikan pelanggan menyala

            // Waktu tunggu agar pelanggan terlihat bersiap di meja
            yield return new WaitForSeconds(3f); 

            // 2. SISTEM RANDOM EVENT FUNGUS (Contoh: Percobaan Suap)
            // Cek apakah pelanggan ini memiliki event dan apakah probabilitasnya tembus
            if (currentCustomer.canTriggerEvent && Random.value <= currentCustomer.eventProbability)
            {
                Debug.Log("Random Event Terpicu: " + currentCustomer.fungusMessageToTrigger);
                
                // Kirim pesan ke Flowchart Fungus untuk memulai dialog
                Flowchart.BroadcastFungusMessage(currentCustomer.fungusMessageToTrigger);
                
                // Hentikan proses kemunculan kertas di C# karena Fungus mengambil alih layar
                yield break; 
            }

            // 3. Jika tidak ada event Fungus, langsung keluarkan kertas
            ShowPaperAndStartTimer();
        }
        else
        {
            Debug.LogError("Database Pelanggan kosong!");
        }
    }

    // Fungsi ini dipisah agar bisa dipanggil kembali oleh Fungus (Invoke Method)
    // saat dialog selesai atau pemain menolak/menerima suap.
    public void ShowPaperAndStartTimer()
    {
        if (currentCustomer.possibleProducts.Length > 0)
        {
            currentProduct = currentCustomer.possibleProducts[Random.Range(0, currentCustomer.possibleProducts.Length)];
            
            // Masukkan data ke UI Kertas
            namaProdukText.text = "Pengaju: " + currentProduct.namaProduk;
            gulaText.text = "Gula: " + currentProduct.gula + "g";
            garamText.text = "Garam: " + currentProduct.garam + "g";
            lemakText.text = "Lemak: " + currentProduct.lemak + "g";
        }

        // Nyalakan Kertas dan Teks secara eksplisit
        paperContainer.SetActive(true);
        namaProdukText.gameObject.SetActive(true);
        gulaText.gameObject.SetActive(true);
        garamText.gameObject.SetActive(true);
        lemakText.gameObject.SetActive(true);
        
        // Lapor ke GameManager bahwa game bisa dilanjutkan (timer mulai)
        GameManager.Instance.OnCustomerReady(currentProduct);
    }

    // --------------------------------------------------------
    // FASE 2: HASIL KEPUTUSAN & FADE OUT
    // --------------------------------------------------------
    public void EndCustomerSequence(bool isHappy)
    {
        StartCoroutine(EndSequence(isHappy));
    }

    private IEnumerator EndSequence(bool isHappy)
    {
        // Ubah ekspresi berdasarkan keputusan benar/salah
        customerRenderer.sprite = isHappy ? currentCustomer.spriteSenang : currentCustomer.spriteMarah;
        
        yield return new WaitForSeconds(1.5f); // Jeda sebelum memudar

        float fadeDuration = 1f;
        float timeElapsed = 0f;
        Color pColor = paperSprite.color;
        Color cColor = customerRenderer.color;

        // Efek memudar secara perlahan
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

        // Matikan objek secara spesifik dan total
        customerObject.SetActive(false); 
        paperContainer.SetActive(false); 
        
        namaProdukText.gameObject.SetActive(false);
        gulaText.gameObject.SetActive(false);
        garamText.gameObject.SetActive(false);
        lemakText.gameObject.SetActive(false);

        // Lapor ke GameManager bahwa meja sudah kosong dan siap untuk diproses
        GameManager.Instance.CheckDayProgress();
    }

    private void ResetFadeColors()
    {
        Color pColor = paperSprite.color; pColor.a = 1f; paperSprite.color = pColor;
        Color cColor = customerRenderer.color; cColor.a = 1f; customerRenderer.color = cColor;
        if (paperUI != null) paperUI.alpha = 1f;
    }
}