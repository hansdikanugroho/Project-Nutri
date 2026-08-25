using UnityEngine;
using UnityEngine.InputSystem;

public class StampController : MonoBehaviour
{
    public NutriLevel stampLevel;
    public Transform targetPaperArea;
    public float snapRadius = 1.5f;
    
    private Vector3 startPos;
    private bool isDragging = false;
    private Camera mainCam;

    void Start()
    {
        startPos = transform.position;
        mainCam = Camera.main; 
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // KONDISI UTAMA: Jika kertas sudah dicap, matikan interaksi stamp!
        if (GameManager.Instance.sudahDiCap)
        {
            isDragging = false; 
            return; // Cegah script membaca input mouse di bawah ini
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
            }
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector3 mousePosition = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePosition.z = 0; 
            transform.position = mousePosition;
        }

        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            
            if (Vector2.Distance(transform.position, targetPaperArea.position) <= snapRadius)
            {
                GameManager.Instance.ProcessDecision(stampLevel);
            }
            
            transform.position = startPos; 
        }
    }
}