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

    [Tooltip("Mencegah sprite idle/drag terlihat gepeng atau memanjang.")]
    [SerializeField] private bool preserveSpriteAspect = true;

    [Tooltip("Aktifkan agar ukuran stamp saat di-drag bisa diatur dari Inspector.")]
    [SerializeField] private bool useCustomDragSize = true;

    [Tooltip("Ukuran RectTransform stamp saat di-drag.")]
    [SerializeField] private Vector2 dragSize = new Vector2(150f, 150f);

    [Header("Shadow Settings")]
    [Tooltip("Boleh dikosongkan. Shadow akan dicari otomatis dari sibling yang namanya mengandung 'Shadow'.")]
    [SerializeField] private RectTransform shadowRect;

    [Tooltip("Jarak shadow dari stamp saat di-drag, dalam satuan UI Canvas.")]
    [SerializeField] private Vector2 dragShadowOffset = new Vector2(10f, -10f);

    [Tooltip("Gunakan sprite drag juga sebagai bentuk shadow agar tidak tetap berbentuk stamp berdiri.")]
    [SerializeField] private bool shadowUsesDragSprite = true;

    [Header("Drop Target Feedback")]
    [Range(0f, 1f)] public float highlightStrength = 0.45f;

    private RectTransform rect;
    private Image stampImage;
    private Image targetImage;
    private Color targetBaseColor;
    private Color targetHighlightColor;

    private Vector2 startAnchoredPosition;
    private Vector2 startSizeDelta;
    private Vector3 startLocalScale;
    private int startSiblingIndex;
    private Transform startParent;

    private Image shadowImage;
    private Transform shadowStartParent;
    private Vector2 shadowStartAnchoredPosition;
    private Vector2 shadowStartSizeDelta;
    private Vector3 shadowStartLocalScale;
    private Quaternion shadowStartLocalRotation;
    private int shadowStartSiblingIndex;
    private Sprite shadowStartSprite;

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
        startSizeDelta = rect.sizeDelta;
        startLocalScale = rect.localScale;
        startSiblingIndex = rect.GetSiblingIndex();
        startParent = rect.parent;

        FindAndCacheShadow();

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        ResolveDropArea();

        if (stampImage != null)
        {
            stampImage.raycastTarget = true;
            stampImage.preserveAspect = preserveSpriteAspect;
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
        if (TutorialFlowController.Instance != null)
            return !TutorialFlowController.Instance.HasDecided;
        return GameManager.Instance != null && !GameManager.Instance.sudahDiCap;
    }

    private bool CanAcceptDrop()
    {
        if (dropArea != null) return dropArea.CanAcceptDrop();

        if (targetStampArea == null) return false;
        if (!targetStampArea.gameObject.activeInHierarchy) return false;
        if (TutorialFlowController.Instance != null)
            return TutorialFlowController.Instance.IsProcessing;
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
            stampImage.preserveAspect = preserveSpriteAspect;
        }

        ApplyDragSize();
        ApplyDragShadowVisual();
        SyncShadowPosition();

        SetHighlight(IsOverTarget(eventData.position) && CanAcceptDrop());
    }

    private void PromoteToCanvasTop()
    {
        if (canvasRect == null) return;

        if (shadowRect != null && shadowRect.parent != canvasRect)
        {
            shadowRect.SetParent(canvasRect, true);
        }
        if (shadowRect != null) shadowRect.SetAsLastSibling();

        if (rect.parent != canvasRect)
        {
            rect.SetParent(canvasRect, true);
        }
        rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || rect == null) return;

        Vector3 worldPoint;
        if (TryScreenToCanvasWorld(eventData.position, out worldPoint))
        {
            rect.position = worldPoint + grabOffset;
            SyncShadowPosition();
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
        if (TutorialFlowController.Instance != null)
        {
            TutorialFlowController.Instance.ProcessDecision(stampLevel);
            return;
        }

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
        rect.sizeDelta = startSizeDelta;
        rect.localScale = startLocalScale;

        RestoreShadow();
    }

    private void FindAndCacheShadow()
    {
        if (shadowRect == null && rect.parent != null)
        {
            string expectedName = name.Trim() + " Shadow";
            Transform parent = rect.parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform candidate = parent.GetChild(i);
                if (candidate == transform) continue;

                bool nameMatches = candidate.name.Equals(expectedName, System.StringComparison.OrdinalIgnoreCase)
                    || candidate.name.IndexOf("shadow", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (nameMatches)
                {
                    shadowRect = candidate as RectTransform;
                    if (shadowRect != null) break;
                }
            }
        }

        if (shadowRect == null) return;

        shadowImage = shadowRect.GetComponent<Image>();
        shadowStartParent = shadowRect.parent;
        shadowStartAnchoredPosition = shadowRect.anchoredPosition;
        shadowStartSizeDelta = shadowRect.sizeDelta;
        shadowStartLocalScale = shadowRect.localScale;
        shadowStartLocalRotation = shadowRect.localRotation;
        shadowStartSiblingIndex = shadowRect.GetSiblingIndex();
        shadowStartSprite = shadowImage != null ? shadowImage.sprite : null;

        if (shadowImage != null)
        {
            shadowImage.raycastTarget = false;
            shadowImage.preserveAspect = preserveSpriteAspect;
        }
    }

    private void ApplyDragSize()
    {
        if (!useCustomDragSize) return;

        rect.sizeDelta = new Vector2(
            Mathf.Max(1f, dragSize.x),
            Mathf.Max(1f, dragSize.y));
    }

    private void ApplyDragShadowVisual()
    {
        if (shadowRect == null) return;

        shadowRect.sizeDelta = rect.sizeDelta;
        shadowRect.rotation = rect.rotation;

        if (shadowImage != null)
        {
            if (shadowUsesDragSprite && dragSprite != null)
            {
                shadowImage.sprite = dragSprite;
            }
            shadowImage.preserveAspect = preserveSpriteAspect;
        }
    }

    private void SyncShadowPosition()
    {
        if (shadowRect == null || rect == null) return;

        Vector3 worldOffset = canvasRect != null
            ? canvasRect.TransformVector(new Vector3(dragShadowOffset.x, dragShadowOffset.y, 0f))
            : new Vector3(dragShadowOffset.x, dragShadowOffset.y, 0f);

        shadowRect.position = rect.position + worldOffset;
    }

    private void RestoreShadow()
    {
        if (shadowRect == null) return;

        if (shadowStartParent != null && shadowRect.parent != shadowStartParent)
        {
            shadowRect.SetParent(shadowStartParent, true);
        }

        shadowRect.SetSiblingIndex(shadowStartSiblingIndex);
        shadowRect.anchoredPosition = shadowStartAnchoredPosition;
        shadowRect.sizeDelta = shadowStartSizeDelta;
        shadowRect.localScale = shadowStartLocalScale;
        shadowRect.localRotation = shadowStartLocalRotation;

        if (shadowImage != null)
        {
            shadowImage.sprite = shadowStartSprite;
            shadowImage.raycastTarget = false;
            shadowImage.preserveAspect = preserveSpriteAspect;
        }
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
