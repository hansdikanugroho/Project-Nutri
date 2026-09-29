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
    private Vector3 startLocalScale;
    private int startSiblingIndex;
    private Transform startParent;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;

    private StampDropArea dropArea;

    private Vector3 grabOffset;
    private bool isDragging;
    private bool isOverTarget;
    private bool dropHandled;
    private bool resolveDone;
    private bool visualCached;

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
        startLocalScale = rect.localScale;
        startSiblingIndex = rect.GetSiblingIndex();
        startParent = rect.parent;

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        ResolveDropArea();

        if (stampImage != null)
        {
            stampImage.raycastTarget = true;
            if (idleSprite != null) stampImage.sprite = idleSprite;
        }
    }

    void OnEnable()
    {
        ResolveDropArea();
    }

    void OnDisable()
    {
        CancelDrag();
    }

    void Update()
    {
        if (!resolveDone)
        {
            ResolveDropArea();
            resolveDone = dropArea != null || targetStampArea != null;
        }

        if (!isDragging) return;

        if (!CanInteract() || rect == null)
        {
            CancelDrag();
        }
    }

    private void ResolveDropArea()
    {
        if (dropArea != null) return;

        Canvas searchRoot = canvas != null ? canvas : GetComponentInParent<Canvas>();

        if (targetStampArea != null)
        {
            dropArea = targetStampArea.GetComponent<StampDropArea>();
        }

        if (dropArea == null)
        {
            dropArea = StampDropArea.Instance;
        }

        if (dropArea == null && searchRoot != null)
        {
            dropArea = searchRoot.GetComponentInChildren<StampDropArea>(true);
        }

        if (dropArea != null && targetStampArea == null)
        {
            targetStampArea = dropArea.transform as RectTransform;
        }

        if (targetStampArea == null)
        {
            targetStampArea = FindRectByName(searchRoot, "StampArea");
        }

        if (dropArea == null && targetStampArea != null)
        {
            dropArea = targetStampArea.GetComponent<StampDropArea>();
            if (dropArea == null)
            {
                dropArea = targetStampArea.gameObject.AddComponent<StampDropArea>();
            }
        }

        CacheTargetVisual();
    }

    private static RectTransform FindRectByName(Canvas root, string objectName)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.name != objectName) continue;
            if (t is RectTransform rect && (root == null || rect.IsChildOf(root.transform) || rect == root.transform))
            {
                return rect;
            }
        }
        return null;
    }

    private void CacheTargetVisual()
    {
        if (targetStampArea == null || visualCached) return;

        targetImage = targetStampArea.GetComponent<Image>();
        if (targetImage == null) return;

        visualCached = true;

        if (dropArea != null)
        {
            targetImage.raycastTarget = true;
        }

        targetBaseColor = targetImage.color;
        targetHighlightColor = Color.Lerp(targetBaseColor, Color.green, highlightStrength);
        targetHighlightColor.a = targetBaseColor.a;
    }

    private bool CanInteract()
    {
        if (Time.timeScale == 0f) return false;
        return GameManager.Instance != null && !GameManager.Instance.sudahDiCap;
    }

    private bool CanAcceptDrop()
    {
        if (dropArea != null) return dropArea.CanAcceptDrop();

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
        dropHandled = false;

        PromoteToCanvasTop();

        if (stampImage != null)
        {
            if (dragSprite != null) stampImage.sprite = dragSprite;
            stampImage.color = new Color(1f, 1f, 1f, stampImage.color.a);
            stampImage.raycastTarget = false;
        }

        SetHighlight(IsOverTarget(eventData.position) && CanAcceptDrop());
    }

    private void PromoteToCanvasTop()
    {
        if (canvasRect == null) return;
        if (rect.parent == canvasRect) return;

        rect.SetParent(canvasRect, true);
        rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || rect == null) return;

        Vector3 worldPoint;
        if (TryScreenToCanvasWorld(eventData.position, out worldPoint))
        {
            rect.position = worldPoint + grabOffset;
        }

        SetHighlight(IsOverTarget(eventData.position) && CanAcceptDrop());
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        bool shouldStamp = !dropHandled
            && IsOverTarget(eventData.position)
            && CanAcceptDrop();

        isDragging = false;
        dropHandled = false;
        ReturnToStart();
        SetHighlight(false);

        if (shouldStamp)
        {
            CommitStamp();
        }
    }

    public void TryAcceptDrop(StampDropArea area)
    {
        if (!isDragging && !isOverTarget) return;
        if (!CanAcceptDrop()) return;

        dropHandled = true;
        CommitStamp();
    }

    private void CommitStamp()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.ProcessDecision(stampLevel);
    }

    private void CancelDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        dropHandled = false;
        ReturnToStart();
        SetHighlight(false);
    }

    private void ReturnToStart()
    {
        if (stampImage != null)
        {
            if (idleSprite != null) stampImage.sprite = idleSprite;
            stampImage.raycastTarget = true;
        }

        if (rect == null) return;

        if (startParent != null && rect.parent != startParent)
        {
            rect.SetParent(startParent, true);
        }

        rect.SetSiblingIndex(startSiblingIndex);
        rect.anchoredPosition = startAnchoredPosition;
        rect.localScale = startLocalScale;
    }

    private void SetHighlight(bool value)
    {
        if (value == isOverTarget) return;

        isOverTarget = value;

        if (dropArea != null)
        {
            dropArea.SetHighlight(value);
        }
        else if (targetImage != null)
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
