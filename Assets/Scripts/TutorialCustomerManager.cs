using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialCustomerManager : MonoBehaviour
{
    public static TutorialCustomerManager Instance;

    [Header("Visual References")]
    public GameObject paperContainer;
    public Image paperImage;
    public Image stampAreaImage;
    public CanvasGroup paperUI;
    public Button rejectButton;
    public Button investigateButton;

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

    private CustomerData currentCustomer;
    private ProductData currentProduct;
    private bool hasInvestigated;

    void Awake()
    {
        Instance = this;
        if (rejectButton != null) rejectButton.onClick.AddListener(OnRejectButtonClicked);
        if (investigateButton != null) investigateButton.onClick.AddListener(OnInvestigateButtonClicked);
        SetActionButtons(false);
        ResetVisuals();
    }

    public void SpawnCustomer(CustomerData customer, bool showActionButtons)
    {
        if (customer == null || customer.possibleProducts == null || customer.possibleProducts.Length == 0) return;
        StopAllCoroutines();
        currentCustomer = customer;
        currentProduct = customer.possibleProducts[0];
        hasInvestigated = false;
        SetActionButtons(showActionButtons);
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        ResetVisuals();
        if (customerRenderer != null)
        {
            customerRenderer.sprite = currentCustomer.spriteNetral;
            Color color = customerRenderer.color;
            color.a = 0f;
            customerRenderer.color = color;
        }
        if (customerObject != null) customerObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            if (customerRenderer != null)
            {
                Color color = customerRenderer.color;
                color.a = Mathf.Clamp01(elapsed / 0.5f);
                customerRenderer.color = color;
            }
            yield return null;
        }

        ShowPaper();
    }

    private void ShowPaper()
    {
        SetText(namaProdukText, currentProduct.namaProduk);
        SetText(produsenText, currentProduct.asalProdusen);
        SetText(kategoriText, currentProduct.kategoriProduk);
        SetText(gulaText, currentProduct.gula + " g");
        SetText(garamText, currentProduct.garam + " mg");
        SetText(lemakText, currentProduct.lemak + " g");
        if (kandunganBerbahayaText != null) kandunganBerbahayaText.gameObject.SetActive(false);
        if (foodRenderer != null) foodRenderer.sprite = currentProduct.gambarMakanan;
        if (foodObject != null) foodObject.SetActive(true);
        if (paperContainer != null) paperContainer.SetActive(true);
        if (paperUI != null)
        {
            paperUI.alpha = 1f;
            paperUI.interactable = true;
            paperUI.blocksRaycasts = true;
        }
        if (audioSource != null && paperSpawnSFX != null) audioSource.PlayOneShot(paperSpawnSFX);
        TutorialFlowController.Instance?.OnCustomerReady(currentProduct);
    }

    public void OnInvestigateButtonClicked()
    {
        if (hasInvestigated || TutorialFlowController.Instance == null || TutorialFlowController.Instance.HasDecided) return;
        hasInvestigated = true;
        if (customerRenderer != null) customerRenderer.sprite = currentCustomer.spriteCemas;
        if (kandunganBerbahayaText != null)
        {
            kandunganBerbahayaText.text = currentProduct.kandunganBerbahaya;
            kandunganBerbahayaText.gameObject.SetActive(!string.IsNullOrEmpty(currentProduct.kandunganBerbahaya));
        }
        TutorialFlowController.Instance.NotifyInvestigateClicked();
    }

    public void OnRejectButtonClicked()
    {
        TutorialFlowController.Instance?.RejectProduct();
    }

    public void EndCustomerSequence(bool isHappy)
    {
        StartCoroutine(EndSequence(isHappy));
    }

    private IEnumerator EndSequence(bool isHappy)
    {
        if (customerRenderer != null)
            customerRenderer.sprite = isHappy ? currentCustomer.spriteSenang : currentCustomer.spriteMarah;
        yield return new WaitForSeconds(0.75f);
        ResetVisuals();
    }

    private void SetActionButtons(bool isVisible)
    {
        if (investigateButton != null)
        {
            investigateButton.gameObject.SetActive(isVisible);
            investigateButton.enabled = isVisible;
        }
        if (rejectButton != null)
        {
            rejectButton.gameObject.SetActive(isVisible);
            rejectButton.enabled = isVisible;
        }
    }

    private void ResetVisuals()
    {
        if (paperContainer != null) paperContainer.SetActive(false);
        if (customerObject != null) customerObject.SetActive(false);
        if (foodObject != null) foodObject.SetActive(false);
        if (kandunganBerbahayaText != null) kandunganBerbahayaText.gameObject.SetActive(false);
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }

    void OnDestroy()
    {
        if (rejectButton != null) rejectButton.onClick.RemoveListener(OnRejectButtonClicked);
        if (investigateButton != null) investigateButton.onClick.RemoveListener(OnInvestigateButtonClicked);
        if (Instance == this) Instance = null;
    }
}
