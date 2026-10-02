using UnityEngine;
using UnityEngine.UI;

public class GGLIntroController : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    public GameObject panelKecil;

    private float resumeTimeScale = 1f;
    private bool isPausingGame;

    private void Awake()
    {
        transform.localScale = Vector3.one;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseIntro);
        }
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
        if (panelKecil != null) panelKecil.SetActive(false);

        PauseGame();
    }

    private void PauseGame()
    {
        if (isPausingGame) return;

        resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        isPausingGame = true;
    }

    public void OpenLargePanel()
    {
        transform.localScale = Vector3.one;
        if (panelKecil != null) panelKecil.SetActive(false);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        PauseGame();
    }

    public void CloseIntro()
    {
        if (!isPausingGame) return;

        isPausingGame = false;
        Time.timeScale = resumeTimeScale;
        gameObject.SetActive(false);
        if (panelKecil != null) panelKecil.SetActive(true);
    }

    private void OnDisable()
    {
        // Tetap pulihkan waktu bila panel dimatikan dari Inspector/script lain.
        if (!isPausingGame) return;

        isPausingGame = false;
        Time.timeScale = resumeTimeScale;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseIntro);
        }

        if (isPausingGame)
        {
            Time.timeScale = resumeTimeScale;
        }
    }
}
