using UnityEngine;
using UnityEngine.InputSystem;

public class StampController : MonoBehaviour
{
    public NutriLevel stampLevel;
    public Transform targetPaperArea;
    public float snapRadius = 1.5f;

    [Header("Sprite Settings")]
    public Sprite idleSprite; // Gambar stempel saat diam di meja
    public Sprite dragSprite; // Gambar stempel saat sedang diangkat/di-drag

    private SpriteRenderer spriteRenderer;
    private Vector3 startPos;
    private bool isDragging = false;
    private Camera mainCam;

    void Start()
    {
        startPos = transform.position;
        mainCam = Camera.main; 
        
        // Ambil komponen SpriteRenderer yang ada di objek ini
        spriteRenderer = GetComponent<SpriteRenderer>(); 
        
        // Pastikan sprite awal adalah sprite saat di meja
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // KONDISI UTAMA: Jika kertas sudah dicap, matikan interaksi stamp!
        if (GameManager.Instance.sudahDiCap)
        {
            // Jika sedang di-drag lalu tiba-tiba game selesai dicap, kembalikan posisi & gambar
            if (isDragging)
            {
                isDragging = false;
                if (spriteRenderer != null && idleSprite != null) spriteRenderer.sprite = idleSprite;
                transform.position = startPos;
            }
            return; // Cegah script membaca input mouse di bawah ini
        }

        // MOUSE DIKLIK
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
                
                // Ganti gambar menjadi mode diangkat
                if (spriteRenderer != null && dragSprite != null)
                {
                    spriteRenderer.sprite = dragSprite;
                }
            }
        }

        // MOUSE DITAHAN (DRAG)
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector3 mousePosition = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePosition.z = 0; 
            transform.position = mousePosition;
        }

        // MOUSE DILEPAS
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            
            // Kembalikan gambar ke mode diam di meja
            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
            
            if (Vector2.Distance(transform.position, targetPaperArea.position) <= snapRadius)
            {
                GameManager.Instance.ProcessDecision(stampLevel);
            }
            
            transform.position = startPos; 
        }
    }
}