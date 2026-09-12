using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

[System.Serializable]
public class TimeSetting
{
    [Header("Global Light (Suasana)")]
    public Color globalColor = Color.white;
    public float globalIntensity = 1f;

    [Header("Freeform Light (Warna Sinar)")]
    public Color rayColor = Color.white;
    public float rayIntensity = 1f;

    [Header("Freeform Light (Posisi & Rotasi)")]
    public Vector3 rayPosition; 
    public Vector3 rayRotation; 

    [Header("Freeform Light (Bentuk Sinar)")]
    public float rayFalloff = 1.75f; 
    public float rayFalloffStrength = 0f;
}

public class DayNightController : MonoBehaviour
{
    [Header("Komponen Target")]
    public Light2D[] globalLights; // Pakai Array biar bisa masukin 2 Global Light
    public Light2D freeformLight;

    [Header("Durasi Transisi (Detik)")]
    public float transitionTime = 2f;

    [Header("Setting 4 Waktu")]
    public TimeSetting pagi;
    public TimeSetting siang;
    public TimeSetting sore;
    public TimeSetting malam;

    void Start()
    {
        // Terapkan Pagi di awal
        foreach(var gl in globalLights)
        {
            if(gl != null)
            {
                gl.color = new Color(pagi.globalColor.r, pagi.globalColor.g, pagi.globalColor.b, 1f);
                gl.intensity = pagi.globalIntensity;
            }
        }

        if (freeformLight != null)
        {
            freeformLight.color = new Color(pagi.rayColor.r, pagi.rayColor.g, pagi.rayColor.b, 1f);
            freeformLight.intensity = pagi.rayIntensity;
            freeformLight.transform.localPosition = pagi.rayPosition;
            freeformLight.transform.localEulerAngles = pagi.rayRotation;
            freeformLight.shapeLightFalloffSize = pagi.rayFalloff;
            freeformLight.falloffIntensity = pagi.rayFalloffStrength;
        }
    }

    // Fungsi baru untuk dipanggil oleh CustomerManager
    public void SetTimePhase(int phaseIndex)
    {
        if (phaseIndex == 0) StartTransition(pagi);
        else if (phaseIndex == 1) StartTransition(siang);
        else if (phaseIndex == 2) StartTransition(sore);
        else if (phaseIndex >= 3) StartTransition(malam);
    }

    private void StartTransition(TimeSetting targetTime)
    {
        StopAllCoroutines();
        StartCoroutine(FadeToTime(targetTime));
    }

    private IEnumerator FadeToTime(TimeSetting target)
    {
        if (freeformLight == null) yield break;

        Color[] startGlobalColors = new Color[globalLights.Length];
        float[] startGlobalInts = new float[globalLights.Length];

        for (int i = 0; i < globalLights.Length; i++)
        {
            if (globalLights[i] != null)
            {
                startGlobalColors[i] = globalLights[i].color;
                startGlobalInts[i] = globalLights[i].intensity;
            }
        }
        
        Color startRayColor = freeformLight.color;
        float startRayInt = freeformLight.intensity;
        Vector3 startRayPos = freeformLight.transform.localPosition;
        Vector3 startRayRot = freeformLight.transform.localEulerAngles;
        float startRayFalloff = freeformLight.shapeLightFalloffSize;
        float startRayFalloffStrength = freeformLight.falloffIntensity;

        float elapsedTime = 0;

        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;

            for (int i = 0; i < globalLights.Length; i++)
            {
                if (globalLights[i] != null)
                {
                    globalLights[i].color = new Color(
                        Mathf.Lerp(startGlobalColors[i].r, target.globalColor.r, t),
                        Mathf.Lerp(startGlobalColors[i].g, target.globalColor.g, t),
                        Mathf.Lerp(startGlobalColors[i].b, target.globalColor.b, t),
                        1f
                    );
                    globalLights[i].intensity = Mathf.Lerp(startGlobalInts[i], target.globalIntensity, t);
                }
            }

            freeformLight.color = new Color(
                Mathf.Lerp(startRayColor.r, target.rayColor.r, t),
                Mathf.Lerp(startRayColor.g, target.rayColor.g, t),
                Mathf.Lerp(startRayColor.b, target.rayColor.b, t),
                1f
            );
            freeformLight.intensity = Mathf.Lerp(startRayInt, target.rayIntensity, t);

            freeformLight.transform.localPosition = Vector3.Lerp(startRayPos, target.rayPosition, t);
            
            float rotX = Mathf.LerpAngle(startRayRot.x, target.rayRotation.x, t);
            float rotY = Mathf.LerpAngle(startRayRot.y, target.rayRotation.y, t);
            float rotZ = Mathf.LerpAngle(startRayRot.z, target.rayRotation.z, t);
            freeformLight.transform.localEulerAngles = new Vector3(rotX, rotY, rotZ);

            freeformLight.shapeLightFalloffSize = Mathf.Lerp(startRayFalloff, target.rayFalloff, t);
            freeformLight.falloffIntensity = Mathf.Lerp(startRayFalloffStrength, target.rayFalloffStrength, t);

            yield return null; 
        }
    }
}