using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public static class PaperRevealEffect
{
    public static IEnumerator RevealField(TextMeshProUGUI label, string oldValue, string newValue, float strikeDelay, float charsPerSecond)
    {
        if (label == null) yield break;

        label.text = "<s>" + oldValue + "</s>";
        yield return new WaitForSeconds(strikeDelay);

        label.text = string.Empty;

        float delay = 1f / Mathf.Max(charsPerSecond, 1f);
        var sb = new StringBuilder(newValue);

        for (int i = 0; i < sb.Length; i++)
        {
            label.text = sb.ToString(0, i + 1);
            yield return new WaitForSeconds(delay);
        }

        label.text = newValue;
    }

    public static IEnumerator Shake(RectTransform target, float duration, float magnitude, Vector2 basePosition)
    {
        if (target == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float falloff = 1f - t;
            float offsetX = (Random.value * 2f - 1f) * magnitude * falloff;
            float offsetY = (Random.value * 2f - 1f) * magnitude * falloff;
            target.anchoredPosition = basePosition + new Vector2(offsetX, offsetY);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.anchoredPosition = basePosition;
    }
}
