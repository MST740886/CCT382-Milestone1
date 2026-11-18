using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class ArtworkDissolve : MonoBehaviour
{
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    [SerializeField, Min(0.1f)] private float dissolveDuration = 1.25f;

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private bool isDissolving;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        SetDissolveAmount(0f);
    }

    public void BeginDissolve()
    {
        if (isDissolving || !gameObject.activeInHierarchy)
            return;

        if (targetRenderer == null || !targetRenderer.sharedMaterial.HasProperty(DissolveAmountId))
        {
            Debug.LogError("ArtworkDissolve requires a material using ArtworkDissolve.shadergraph.", this);
            return;
        }

        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        isDissolving = true;
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);
            SetDissolveAmount(Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        SetDissolveAmount(1f);
        gameObject.SetActive(false);
    }

    private void SetDissolveAmount(float value)
    {
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(DissolveAmountId, value);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
