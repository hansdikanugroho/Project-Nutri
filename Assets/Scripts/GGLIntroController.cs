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
        resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        transform.localScale = Vector3.one;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseIntro);
        }

        Time.timeScale = 0f;
        isPausingGame = true;
    }

    public void CloseIntro()
    {
        if (!isPausingGame) return;

        isPausingGame = false;
        Time.timeScale = resumeTimeScale;
        gameObject.SetActive(false);
        if (panelKecil != null) panelKecil.SetActive(true);
    }

    public void OpenLargePanel()
    {
        gameObject.SetActive(true);
        if (panelKecil != null) panelKecil.SetActive(false);
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
