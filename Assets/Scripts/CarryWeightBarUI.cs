using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a UI Scrollbar synchronized with the shared carry-weight ScriptableObject.
/// </summary>
[DisallowMultipleComponent]
public sealed class CarryWeightBarUI : MonoBehaviour
{
    [Header("Shared carry-weight data")]
    [SerializeField] private FloatVariableSO currentCarryWeight;
    [SerializeField] private GameEventSO onCarryWeightChanged;

    [Header("UI")]
    [SerializeField] private Scrollbar carryWeightBar;
    [SerializeField, Min(0.01f)] private float maxCarryWeight = 10f;

    private void OnEnable()
    {
        if (carryWeightBar == null)
            carryWeightBar = GetComponent<Scrollbar>();

        if (carryWeightBar != null)
            carryWeightBar.interactable = false;

        if (onCarryWeightChanged != null)
            onCarryWeightChanged.RegisterListener(SyncBar);

        SyncBar();
    }

    private void OnDisable()
    {
        if (onCarryWeightChanged != null)
            onCarryWeightChanged.UnregisterListener(SyncBar);
    }

    private void SyncBar()
    {
        if (carryWeightBar == null || currentCarryWeight == null)
            return;

        float capacity = Mathf.Max(0.01f, maxCarryWeight);
        carryWeightBar.value = Mathf.Clamp01(currentCarryWeight.value / capacity);
    }

    /// <summary>Refreshes the display from the shared data without changing it.</summary>
    public void RefreshFromCurrentWeight()
    {
        SyncBar();
    }
}
