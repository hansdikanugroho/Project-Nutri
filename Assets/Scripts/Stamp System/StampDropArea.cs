using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StampDropArea : MonoBehaviour, IDropHandler, IDragHandler
{
    public static StampDropArea Instance { get; private set; }

    [Range(0f, 1f)] public float highlightStrength = 0.45f;
    public bool acceptOnlyWhileProcessing = true;

    private RectTransform rect;
    private Image targetImage;
    private Color baseColor;
    private Color highlightColor;
    private bool highlighted;

    public bool IsReady => rect != null && gameObject.activeInHierarchy;

    void Awake()
    {
        if (Instance == null) Instance = this;

        rect = transform as RectTransform;
        targetImage = GetComponent<Image>();

        if (rect == null)
        {
            Debug.LogError($"[StampDropArea] '{name}' butuh RectTransform.");
            enabled = false;
            return;
        }

        if (targetImage != null)
        {
            targetImage.raycastTarget = true;

            baseColor = targetImage.color;
            highlightColor = Color.Lerp(baseColor, Color.green, highlightStrength);
            highlightColor.a = baseColor.a;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool CanAcceptDrop()
    {
        if (!IsReady) return false;
        if (!acceptOnlyWhileProcessing) return true;

        if (TutorialFlowController.Instance != null)
            return TutorialFlowController.Instance.IsProcessing;

        return GameManager.Instance != null
            && GameManager.Instance.isProcessing
            && !GameManager.Instance.sudahDiCap;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData == null)
        {
            SetHighlight(false);
            return;
        }

        bool isStampDrag = eventData.pointerDrag != null
            && eventData.pointerDrag.GetComponent<StampController>() != null;

        SetHighlight(isStampDrag && CanAcceptDrop());
    }

    public void OnDrop(PointerEventData eventData)
    {
        SetHighlight(false);

        if (eventData == null) return;

        StampController stamp = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<StampController>()
            : null;

        if (stamp == null) return;

        stamp.TryAcceptDrop(this);
    }

    public void SetHighlight(bool value)
    {
        if (value == highlighted) return;

        highlighted = value;
        if (targetImage != null)
        {
            targetImage.color = value ? highlightColor : baseColor;
        }
    }
}
