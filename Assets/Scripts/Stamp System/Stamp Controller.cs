using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StampController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public NutriLevel stampLevel;
    public RectTransform targetStampArea;
    public float dropPadding = 60f;

    [Header("Sprite Settings")]
    public Sprite idleSprite;
    public Sprite dragSprite;

    [Header("Drop Target Feedback")]
    [Range(0f, 1f)] public float highlightStrength = 0.45f;

    private RectTransform rect;
    private Image stampImage;
    private Image targetImage;
    private Color targetBaseColor;
    private Color targetHighlightColor;

    private Vector2 startAnchoredPosition;
    private int startSiblingIndex;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;

    private Vector3 grabOffset;
    private bool isDragging;
    private bool isOverTarget;

    void Awake()
    {
        rect = transform as RectTransform;
        stampImage = GetComponent<Image>();

        if (rect == null)
        {
            Debug.LogError($"[StampController] '{name}' butuh RectTransform (objek harus di dalam Canvas uGUI).");
            enabled = false;
            return;
        }

        startAnchoredPosition = rect.anchoredPosition;
        startSiblingIndex = rect.GetSiblingIndex();

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (targetStampArea != null)
        {
            targetImage = targetStampArea.GetComponent<Image>();
            if (targetImage != null)
            {
                targetBaseColor = targetImage.color;
                targetHighlightColor = Color.Lerp(targetBaseColor, Color.green, highlightStrength);
                targetHighlightColor.a = targetBaseColor.a;
            }
        }

        if (stampImage != null)
        {
            stampImage.raycastTarget = true;
            if (idleSprite != null) stampImage.sprite = idleSprite;
        }
    }

    void OnDisable()
    {
        CancelDrag();
    }

    void Update()
    {
        if (!isDragging) return;

        if (!CanInteract() || rect == null)
        {
            CancelDrag();
        }
    }

    private bool CanInteract()
    {
        if (Time.timeScale == 0f) return false;
        return GameManager.Instance != null && !GameManager.Instance.sudahDiCap;
    }

    private bool CanAcceptDrop()
    {
        if (targetStampArea == null) return false;
        if (!targetStampArea.gameObject.activeInHierarchy) return false;
        return GameManager.Instance != null && GameManager.Instance.isProcessing;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract() || rect == null) return;

        Vector3 worldPoint;
        if (TryScreenToCanvasWorld(eventData.position, out worldPoint))
        {
            grabOffset = rect.position - worldPoint;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanInteract() || rect == null) return;

        isDragging = true;
        rect.SetAsLastSibling();

        if (stampImage != null)
        {
            if (dragSprite != null) stampImage.sprite = dragSprite;
            stampImage.color = new Color(1f, 1f, 1f, stampImage.color.a);
        }

        SetHighlight(IsOverTarget(eventData.position));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || rect == null) return;

        Vector3 worldPoint;
        if (TryScreenToCanvasWorld(eventData.position, out worldPoint))
        {
            rect.position = worldPoint + grabOffset;
        }

        SetHighlight(IsOverTarget(eventData.position));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        bool shouldStamp = IsOverTarget(eventData.position) && CanAcceptDrop();

        isDragging = false;
        ReturnToStart();
        SetHighlight(false);

        if (shouldStamp)
        {
            GameManager.Instance.ProcessDecision(stampLevel);
        }
    }

    private void CancelDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        ReturnToStart();
        SetHighlight(false);
    }

    private void ReturnToStart()
    {
        if (stampImage != null && idleSprite != null) stampImage.sprite = idleSprite;
        if (rect == null) return;

        rect.SetSiblingIndex(startSiblingIndex);
        rect.anchoredPosition = startAnchoredPosition;
    }

    private void SetHighlight(bool value)
    {
        if (value == isOverTarget) return;

        isOverTarget = value;
        if (targetImage != null)
        {
            targetImage.color = value ? targetHighlightColor : targetBaseColor;
        }
    }

    private bool IsOverTarget(Vector2 screenPoint)
    {
        if (targetStampArea == null || !targetStampArea.gameObject.activeInHierarchy) return false;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetStampArea, screenPoint, canvasCamera, out localPoint))
        {
            return false;
        }

        Rect area = targetStampArea.rect;
        return localPoint.x >= area.xMin - dropPadding && localPoint.x <= area.xMax + dropPadding
            && localPoint.y >= area.yMin - dropPadding && localPoint.y <= area.yMax + dropPadding;
    }

    private bool TryScreenToCanvasWorld(Vector2 screenPoint, out Vector3 worldPoint)
    {
        worldPoint = default;

        if (canvasRect == null)
        {
            if (canvas != null)
            {
                worldPoint = canvas.transform.TransformPoint(screenPoint);
                return true;
            }
            return false;
        }

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPoint, canvasCamera, out worldPoint);
    }
}
