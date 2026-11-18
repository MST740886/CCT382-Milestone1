using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArtworkWeightPickup : MonoBehaviour
{
    [SerializeField, Min(0f)] private float itemWeight = 1f;
    [SerializeField] private FloatVariableSO currentCarryWeight;
    [SerializeField] private GameEventSO onCarryWeightChanged;

    private bool collected;

    public bool TryCollect()
    {
        if (collected)
            return false;

        if (currentCarryWeight == null || onCarryWeightChanged == null)
        {
            Debug.LogError("ArtworkWeightPickup needs Current Carry Weight and On Carry Weight Changed assets assigned.", this);
            return false;
        }

        collected = true;
        float addedWeight = Mathf.Max(0f, itemWeight);
        currentCarryWeight.value += addedWeight;
        onCarryWeightChanged.Raise();

        Debug.Log($"Picked up {gameObject.name}: +{addedWeight:0.##} weight; carried weight is {currentCarryWeight.value:0.##}.", this);
        return true;
    }
}
