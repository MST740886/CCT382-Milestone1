using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DamageFlashEffect : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.35f;

    private Image overlay;
    private Coroutine flashRoutine;

    private void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.color = new Color(1f, 0.12f, 0.12f, 0f);
        SetAlpha(0f);
    }

    public void Flash()
    {
        if (overlay == null)
            overlay = GetComponent<Image>();

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(0f, peakAlpha, elapsed / halfDuration));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(peakAlpha, 0f, elapsed / halfDuration));
            yield return null;
        }

        SetAlpha(0f);
        flashRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (overlay == null)
            return;

        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
