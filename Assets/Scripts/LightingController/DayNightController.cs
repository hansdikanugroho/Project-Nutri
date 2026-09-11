using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

// Class khusus buat nampung warna dan intensitas per waktu
[System.Serializable]
public class TimeSetting
{
    [Header("Global Light")]
    public Color globalColor = Color.white;
    public float globalIntensity = 1f;

    [Header("Freeform Light")]
    public Color rayColor = Color.white;
    public float rayIntensity = 1f;
}

public class DayNightController : MonoBehaviour
{
    [Header("Komponen Target")]
    public Light2D globalLight;
    public Light2D freeformLight;

    [Header("Durasi Transisi (Detik)")]
    public float transitionTime = 2f;

    [Header("Setting 4 Waktu")]
    public TimeSetting pagi;
    public TimeSetting siang;
    public TimeSetting sore;
    public TimeSetting malam;

    // Fungsi trigger yang bisa dipanggil kapan aja
    public void SetPagi() => StartTransition(pagi);
    public void SetSiang() => StartTransition(siang);
    public void SetSore() => StartTransition(sore);
    public void SetMalam() => StartTransition(malam);

    private void StartTransition(TimeSetting targetTime)
    {
        // Stop transisi sebelumnya biar nggak bentrok kalau dipanggil beruntun
        StopAllCoroutines();
        StartCoroutine(FadeToTime(targetTime));
    }

    private IEnumerator FadeToTime(TimeSetting target)
    {
        // Tangkap warna dan intensitas lampu saat ini (sebelum transisi dimulai)
        Color startGlobalColor = globalLight.color;
        float startGlobalInt = globalLight.intensity;
        
        Color startRayColor = freeformLight.color;
        float startRayInt = freeformLight.intensity;

        float elapsedTime = 0;

        // Loop untuk memudarkan warna secara halus selama transitionTime
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;

            // Transisi Global Light
            globalLight.color = Color.Lerp(startGlobalColor, target.globalColor, t);
            globalLight.intensity = Mathf.Lerp(startGlobalInt, target.globalIntensity, t);

            // Transisi Freeform Light
            freeformLight.color = Color.Lerp(startRayColor, target.rayColor, t);
            freeformLight.intensity = Mathf.Lerp(startRayInt, target.rayIntensity, t);

            yield return null; // Tunggu ke frame berikutnya
        }
    }
}