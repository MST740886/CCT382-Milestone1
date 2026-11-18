using UnityEditor;
using UnityEngine;

/// <summary>
/// Resets session carry weight whenever a new Play Mode run is started.
/// </summary>
[InitializeOnLoad]
internal static class CarryWeightPlayModeReset
{
    private const string WeightAssetPath = "Assets/Milestone2Data/CurrentCarryWeight.asset";
    private const string WeightChangedEventPath = "Assets/Milestone2Data/OnCarryWeightChanged.asset";
    private const string CurrentHealthAssetPath = "Assets/HealthOS/CurrentHealth.asset";
    private const string MaxHealthAssetPath = "Assets/HealthOS/MaxHealth.asset";

    static CarryWeightPlayModeReset()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        FloatVariableSO currentCarryWeight = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(WeightAssetPath);
        if (currentCarryWeight == null)
            Debug.LogError($"Could not load carry-weight data at '{WeightAssetPath}'.");
        else
        {
            currentCarryWeight.value = 0f;

            GameEventSO weightChangedEvent = AssetDatabase.LoadAssetAtPath<GameEventSO>(WeightChangedEventPath);
            if (weightChangedEvent != null)
                weightChangedEvent.Raise();

            // Enter Play Mode Options can keep scene objects alive, in which case
            // CarryWeightBarUI.OnEnable may not run again. Refresh those displays here.
            CarryWeightBarUI[] weightBars = Object.FindObjectsByType<CarryWeightBarUI>(FindObjectsSortMode.None);
            foreach (CarryWeightBarUI weightBar in weightBars)
                weightBar.RefreshFromCurrentWeight();
        }

        FloatVariableSO currentHealth = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(CurrentHealthAssetPath);
        FloatVariableSO maxHealth = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(MaxHealthAssetPath);
        if (currentHealth == null || maxHealth == null)
        {
            Debug.LogError("Could not load CurrentHealth or MaxHealth ScriptableObject.");
        }
        else
        {
            currentHealth.value = maxHealth.value;

            // Refresh directly instead of raising OnHealthChanged, which would
            // incorrectly play the damage sound at the start of each run.
            HealthBarUI[] healthBars = Object.FindObjectsByType<HealthBarUI>(FindObjectsSortMode.None);
            foreach (HealthBarUI healthBar in healthBars)
                healthBar.RefreshFromCurrentHealth();
        }

        Debug.Log("New Play Mode run: carry weight reset to 0 and health restored to maximum.");
    }
}
