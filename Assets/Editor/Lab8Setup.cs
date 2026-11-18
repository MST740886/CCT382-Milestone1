#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Lab8Setup
{
    private const string HealthFolder = "Assets/HealthOS";
    private const string LegacyEventPath = HealthFolder + "/HealthSO.asset";
    private const string EventPath = HealthFolder + "/OnHealthChanged.asset";
    private const string CurrentHealthPath = HealthFolder + "/CurrentHealth.asset";
    private const string MaxHealthPath = HealthFolder + "/MaxHealth.asset";
    private const string HitSoundPath = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/Player_Land.wav";
    public static void ConfigureHealthUI()
    {
        Scene scene = SceneManager.GetActiveScene();
        HealthDamageTrigger damageTrigger = FindSceneComponent<HealthDamageTrigger>(scene);
        Canvas canvas = FindSceneComponent<Canvas>(scene);
        if (damageTrigger == null || canvas == null)
        {
            Debug.LogError("Lab 8 setup needs the existing HealthDamageTrigger and Canvas in the active scene.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(HealthFolder))
            AssetDatabase.CreateFolder("Assets", "HealthOS");

        if (AssetDatabase.LoadAssetAtPath<GameEventSO>(EventPath) == null &&
            AssetDatabase.LoadAssetAtPath<GameEventSO>(LegacyEventPath) != null)
        {
            string renameError = AssetDatabase.RenameAsset(LegacyEventPath, "OnHealthChanged");
            if (!string.IsNullOrEmpty(renameError))
                Debug.LogWarning("Could not rename legacy HealthSO asset: " + renameError);
        }

        GameEventSO changedEvent = LoadOrCreateEvent(EventPath);
        FloatVariableSO currentHealth = LoadOrCreateFloat(CurrentHealthPath, 50f);
        FloatVariableSO maxHealth = LoadOrCreateFloat(MaxHealthPath, 50f);
        if (changedEvent == null || currentHealth == null || maxHealth == null)
        {
            Debug.LogError("Lab 8 setup could not create the health data assets.");
            return;
        }

        Undo.RecordObject(canvas.transform, "Restore Canvas scale for Lab 8 UI");
        canvas.transform.localScale = Vector3.one;

        Undo.RecordObject(damageTrigger, "Connect Lab 8 health data");
        SerializedObject triggerData = new SerializedObject(damageTrigger);
        triggerData.FindProperty("healthValue").objectReferenceValue = currentHealth;
        triggerData.FindProperty("onHealthChanged").objectReferenceValue = changedEvent;
        triggerData.FindProperty("damageAmount").floatValue = 10f;
        triggerData.ApplyModifiedProperties();

        Slider slider = CreateOrUpdateSlider(canvas.transform);
        HealthBarUI healthBar = CreateOrUpdateHealthBar(canvas.transform);
        AudioSource audioSource = healthBar.GetComponent<AudioSource>();
        AudioClip hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>(HitSoundPath);
        if (hitSound == null)
            Debug.LogWarning("Player_Land.wav was not available; assign an AudioClip to HealthBarUI.changeSound.");

        Undo.RecordObject(healthBar, "Connect Lab 8 health UI");
        SerializedObject uiData = new SerializedObject(healthBar);
        uiData.FindProperty("currentHealth").objectReferenceValue = currentHealth;
        uiData.FindProperty("maxHealth").objectReferenceValue = maxHealth;
        uiData.FindProperty("onHealthChanged").objectReferenceValue = changedEvent;
        uiData.FindProperty("slider").objectReferenceValue = slider;
        uiData.FindProperty("audioSource").objectReferenceValue = audioSource;
        uiData.FindProperty("changeSound").objectReferenceValue = hitSound;
        uiData.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Lab 8 configured: 50/50 health, 10 damage per trigger entry, Slider listener, and hit audio.");
    }

    private static GameEventSO LoadOrCreateEvent(string path)
    {
        GameEventSO asset = AssetDatabase.LoadAssetAtPath<GameEventSO>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<GameEventSO>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static FloatVariableSO LoadOrCreateFloat(string path, float startingValue)
    {
        FloatVariableSO asset = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<FloatVariableSO>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.value = startingValue;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static Slider CreateOrUpdateSlider(Transform canvas)
    {
        Transform existing = canvas.Find("Health Bar");
        GameObject root = existing != null
            ? existing.gameObject
            : new GameObject("Health Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        if (existing == null)
            root.transform.SetParent(canvas, false);

        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(24f, -24f);
        rootRect.sizeDelta = new Vector2(280f, 24f);
        rootRect.localScale = Vector3.one;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.055f, 0.075f, 0.11f, 0.9f);
        background.raycastTarget = false;

        Transform areaTransform = root.transform.Find("Fill Area");
        GameObject area = areaTransform != null ? areaTransform.gameObject : new GameObject("Fill Area", typeof(RectTransform));
        if (areaTransform == null)
            area.transform.SetParent(root.transform, false);
        RectTransform areaRect = area.GetComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = new Vector2(4f, 4f);
        areaRect.offsetMax = new Vector2(-4f, -4f);

        Transform fillTransform = area.transform.Find("Fill");
        GameObject fill = fillTransform != null
            ? fillTransform.gameObject
            : new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (fillTransform == null)
            fill.transform.SetParent(area.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.22f, 0.92f, 0.57f, 1f);
        fillImage.raycastTarget = false;

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.handleRect = null;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    private static HealthBarUI CreateOrUpdateHealthBar(Transform canvas)
    {
        Transform existing = canvas.Find("Health Bar Controller");
        GameObject host = existing != null
            ? existing.gameObject
            : new GameObject("Health Bar Controller", typeof(RectTransform), typeof(AudioSource), typeof(HealthBarUI));
        if (existing == null)
            host.transform.SetParent(canvas, false);

        host.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = host.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        AudioSource source = host.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0.8f;
        return host.GetComponent<HealthBarUI>();
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (T component in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (component.gameObject.scene == scene && component.gameObject.scene.isLoaded)
                return component;
        return null;
    }
}
#endif
