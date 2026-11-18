using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Lab7DissolveSetup
{
    private const string ShaderGraphPath = "Assets/Shader Graphs/ArtworkDissolve.shadergraph";
    private const string MaterialPath = "Assets/ArtworkDissolve.mat";
    private static void ApplyArtworkDissolve()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderGraphPath);
        if (shader == null)
            shader = Shader.Find("Shader Graphs/ArtworkDissolve");
        if (shader == null)
        {
            Debug.LogError("ArtworkDissolve Shader Graph has not compiled yet.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "ArtworkDissolve" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        int configured = 0;
        foreach (GameObject target in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if ((target.name != "Cube" && target.name != "Cube1") || target.layer != 6)
                continue;

            Renderer targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer == null)
                continue;

            Undo.RecordObject(targetRenderer, "Assign artwork dissolve material");
            targetRenderer.sharedMaterial = material;
            if (target.GetComponent<ArtworkDissolve>() == null)
                Undo.AddComponent<ArtworkDissolve>(target);
            EditorUtility.SetDirty(targetRenderer);
            EditorUtility.SetDirty(target);
            configured++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"Lab 7 dissolve configured on {configured} artwork targets. Material: {MaterialPath}");
    }
}
