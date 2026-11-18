#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ArtworkCarryWeightSetup
{
    private const string DataFolder = "Assets/Milestone2Data";
    private const string CarryWeightPath = DataFolder + "/CurrentCarryWeight.asset";
    private const string CarryWeightEventPath = DataFolder + "/OnCarryWeightChanged.asset";
    public static void ConfigureArtworkCarryWeight()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject lab6 = GameObject.Find("Lab6");
        if (lab6 == null)
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            lab6 = GameObject.Find("Lab6");
        }

        if (lab6 == null || lab6.scene != scene)
        {
            Debug.LogError("Milestone 2 setup could not find the Lab6 object in the active scene.");
            return;
        }

        Transform cube = lab6.transform.Find("Cube");
        Transform cube1 = lab6.transform.Find("Cube1");
        if (cube == null || cube1 == null)
        {
            Debug.LogError("Milestone 2 setup expects Lab6/Cube and Lab6/Cube1.");
            return;
        }

        if (!HasArtworkComponents(cube.gameObject) || !HasArtworkComponents(cube1.gameObject))
        {
            Debug.LogError("Both Lab6 artwork objects need a Collider and ArtworkDissolve before carry weight can be configured.");
            return;
        }

        EnsureFolder("Assets", "Milestone2Data");
        FloatVariableSO carryWeight = LoadOrCreateFloat(CarryWeightPath);
        GameEventSO weightChanged = LoadOrCreateEvent(CarryWeightEventPath);
        if (carryWeight == null || weightChanged == null)
        {
            Debug.LogError("Milestone 2 setup could not create the carry-weight ScriptableObject assets.");
            return;
        }

        carryWeight.value = 0f;
        EditorUtility.SetDirty(carryWeight);

        ConfigureItem(cube.gameObject, 2f, carryWeight, weightChanged);
        ConfigureItem(cube1.gameObject, 3f, carryWeight, weightChanged);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Artwork carry weight configured: Cube=2, Cube1=3; shared weight starts at 0. Each pickup raises OnCarryWeightChanged once.");
    }

    private static bool HasArtworkComponents(GameObject target)
    {
        return target.GetComponent<Collider>() != null && target.GetComponent<ArtworkDissolve>() != null;
    }

    private static void ConfigureItem(GameObject target, float defaultWeight, FloatVariableSO carryWeight, GameEventSO weightChanged)
    {
        ArtworkWeightPickup pickup = target.GetComponent<ArtworkWeightPickup>();
        if (pickup == null)
        {
            Undo.AddComponent<ArtworkWeightPickup>(target);
            pickup = target.GetComponent<ArtworkWeightPickup>();
            SerializedObject initialData = new SerializedObject(pickup);
            initialData.FindProperty("itemWeight").floatValue = defaultWeight;
            initialData.ApplyModifiedProperties();
        }

        Undo.RecordObject(pickup, "Connect artwork carry weight");
        SerializedObject itemData = new SerializedObject(pickup);
        itemData.FindProperty("currentCarryWeight").objectReferenceValue = carryWeight;
        itemData.FindProperty("onCarryWeightChanged").objectReferenceValue = weightChanged;
        itemData.ApplyModifiedProperties();
    }

    private static FloatVariableSO LoadOrCreateFloat(string path)
    {
        FloatVariableSO asset = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<FloatVariableSO>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
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

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
