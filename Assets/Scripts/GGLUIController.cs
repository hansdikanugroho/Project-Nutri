using UnityEngine;

public class GGLUIController : MonoBehaviour
{
    public GameObject panelBesar;
    public GameObject panelKecil;

    public void SwitchToSmallPanel()
    {
        if (panelBesar != null) panelBesar.SetActive(false);
        if (panelKecil != null) panelKecil.SetActive(true);
    }

    public void SwitchToLargePanel()
    {
        if (panelBesar != null) panelBesar.SetActive(true);
        if (panelKecil != null) panelKecil.SetActive(false);
    }
}