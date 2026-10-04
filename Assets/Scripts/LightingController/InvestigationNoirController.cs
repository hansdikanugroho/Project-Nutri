using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Efek investigasi ringan untuk URP 2D. Dunia dibuat hitam-putih dengan
/// Volume, sedangkan gambar UI di luar dokumen memakai material grayscale.
/// Isi selectiveColorRoot tidak diubah sehingga dokumen tetap berwarna.
/// </summary>
public class InvestigationNoirController : MonoBehaviour
{
    [Header("Selective Color")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform selectiveColorRoot;
    [SerializeField] private Shader grayscaleUIShader;

    [Header("Sorot Investigation")]
    [SerializeField] private Light2D investigationLight;
    [SerializeField] private Transform spotlightTarget;
    [SerializeField, Min(0f)] private float spotlightIntensity = 2.3f;
    [SerializeField] private Vector2 spotlightOffset = new Vector2(0.3f, 4.15f);

    [Header("Noir Ringan")]
    [SerializeField, Min(0.1f)] private float investigationDuration = 1.1f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Range(-100f, 0f)] private float saturation = -100f;
    [SerializeField, Range(-20f, 20f)] private float contrast = 6f;
    [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.28f;
    [SerializeField, Range(0.01f, 1f)] private float vignetteSmoothness = 0.72f;

    [Header("Reaksi Kecewa")]
    [SerializeField, Min(0f)] private float reactionDuration = 0.22f;
    [SerializeField, Min(0f)] private float reactionMagnitude = 0.06f;

    private readonly Dictionary<Graphic, Material> originalUIMaterials = new Dictionary<Graphic, Material>();
    private Volume noirVolume;
    private VolumeProfile runtimeProfile;
    private Material grayscaleUIMaterial;
    private UniversalAdditionalCameraData cameraData;
    private Coroutine reactionRoutine;
    private Vector3 cameraBasePosition;
    private bool originalPostProcessingState;
    private bool effectActive;

    private static readonly int EffectAmountId = Shader.PropertyToID("_EffectAmount");

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            cameraBasePosition = targetCamera.transform.localPosition;
            cameraData = targetCamera.GetComponent<UniversalAdditionalCameraData>();
        }

        DisableSpotlight();

        CreateRuntimeVolume();

        if (grayscaleUIShader != null)
        {
            grayscaleUIMaterial = new Material(grayscaleUIShader)
            {
                name = "Investigation UI Grayscale (Runtime)"
            };
            grayscaleUIMaterial.SetFloat(EffectAmountId, 0f);
        }
    }

    public IEnumerator PlayInvestigation()
    {
        SetUIGrayscale(true);
        effectActive = true;
        PrepareSpotlight();

        if (cameraData != null)
        {
            originalPostProcessingState = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
        }

        UpdateVignetteCenter();

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float amount = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInDuration);
            SetEffectAmount(amount);
            SetSpotlightAmount(amount);
            yield return null;
        }

        SetEffectAmount(1f);
        SetSpotlightAmount(1f);
        float holdDuration = Mathf.Max(0f, investigationDuration - fadeInDuration);
        if (holdDuration > 0f) yield return new WaitForSecondsRealtime(holdDuration);
    }

    public void StopInvestigationInstant()
    {
        DisableSpotlight();
        if (!effectActive && originalUIMaterials.Count == 0) return;

        SetEffectAmount(0f);
        RestoreUIMaterials();
        effectActive = false;

        if (cameraData != null)
        {
            cameraData.renderPostProcessing = originalPostProcessingState;
        }
    }

    public void PlayDisappointedReaction()
    {
        if (targetCamera == null || reactionDuration <= 0f || reactionMagnitude <= 0f) return;

        if (reactionRoutine != null) StopCoroutine(reactionRoutine);
        reactionRoutine = StartCoroutine(ShakeCameraLightly());
    }

    private void CreateRuntimeVolume()
    {
        noirVolume = gameObject.GetComponent<Volume>();
        if (noirVolume == null) noirVolume = gameObject.AddComponent<Volume>();

        noirVolume.isGlobal = true;
        noirVolume.priority = 100f;
        noirVolume.weight = 0f;

        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.name = "Investigation Noir (Runtime)";
        noirVolume.profile = runtimeProfile;

        ColorAdjustments colorAdjustments = runtimeProfile.Add<ColorAdjustments>();
        colorAdjustments.active = true;
        colorAdjustments.saturation.Override(saturation);
        colorAdjustments.contrast.Override(contrast);

        Vignette vignette = runtimeProfile.Add<Vignette>();
        vignette.active = true;
        vignette.color.Override(Color.black);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.rounded.Override(false);
    }

    private void UpdateVignetteCenter()
    {
        if (runtimeProfile == null) return;
        if (!runtimeProfile.TryGet(out Vignette vignette)) return;

        Vector2 screenPoint;
        if (spotlightTarget != null && targetCamera != null)
        {
            screenPoint = targetCamera.WorldToScreenPoint(spotlightTarget.position + Vector3.up * 0.8f);
        }
        else if (selectiveColorRoot != null)
        {
            screenPoint = RectTransformUtility.WorldToScreenPoint(null, selectiveColorRoot.position);
        }
        else
        {
            screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        Vector2 normalized = new Vector2(
            Screen.width > 0 ? screenPoint.x / Screen.width : 0.5f,
            Screen.height > 0 ? screenPoint.y / Screen.height : 0.5f);

        vignette.center.Override(normalized);
    }

    private void SetUIGrayscale(bool enabled)
    {
        if (!enabled || grayscaleUIMaterial == null) return;

        originalUIMaterials.Clear();
        Graphic[] graphics = FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (!(graphic is Image) && !(graphic is RawImage)) continue;
            if (selectiveColorRoot != null && graphic.transform.IsChildOf(selectiveColorRoot)) continue;

            originalUIMaterials[graphic] = graphic.material;
            graphic.material = grayscaleUIMaterial;
            graphic.SetMaterialDirty();
        }
    }

    private void RestoreUIMaterials()
    {
        foreach (KeyValuePair<Graphic, Material> entry in originalUIMaterials)
        {
            if (entry.Key == null) continue;
            entry.Key.material = entry.Value;
            entry.Key.SetMaterialDirty();
        }

        originalUIMaterials.Clear();
    }

    private void SetEffectAmount(float amount)
    {
        if (noirVolume != null) noirVolume.weight = amount;
        if (grayscaleUIMaterial != null) grayscaleUIMaterial.SetFloat(EffectAmountId, amount);
    }

    private void PrepareSpotlight()
    {
        if (investigationLight == null) return;

        if (spotlightTarget != null)
        {
            Vector3 targetPosition = spotlightTarget.position;
            investigationLight.transform.position = new Vector3(
                targetPosition.x + spotlightOffset.x,
                targetPosition.y + spotlightOffset.y,
                investigationLight.transform.position.z);
        }

        investigationLight.gameObject.SetActive(true);
        investigationLight.enabled = true;
        investigationLight.intensity = 0f;
    }

    private void SetSpotlightAmount(float amount)
    {
        if (investigationLight == null || !investigationLight.gameObject.activeSelf) return;
        investigationLight.intensity = Mathf.SmoothStep(0f, spotlightIntensity, Mathf.Clamp01(amount));
    }

    private void DisableSpotlight()
    {
        if (investigationLight == null) return;
        investigationLight.intensity = 0f;
        investigationLight.enabled = false;
        investigationLight.gameObject.SetActive(false);
    }

    private IEnumerator ShakeCameraLightly()
    {
        Transform cameraTransform = targetCamera.transform;
        cameraBasePosition = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < reactionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float strength = 1f - Mathf.Clamp01(elapsed / reactionDuration);
            Vector2 offset = Random.insideUnitCircle * (reactionMagnitude * strength);
            cameraTransform.localPosition = cameraBasePosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        cameraTransform.localPosition = cameraBasePosition;
        reactionRoutine = null;
    }

    private void OnDisable()
    {
        StopInvestigationInstant();
        if (targetCamera != null) targetCamera.transform.localPosition = cameraBasePosition;
    }

    private void OnDestroy()
    {
        RestoreUIMaterials();
        if (grayscaleUIMaterial != null) Destroy(grayscaleUIMaterial);
        if (runtimeProfile != null) Destroy(runtimeProfile);
    }
}
