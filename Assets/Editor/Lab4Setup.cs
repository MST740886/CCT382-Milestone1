using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class Lab4Setup
{
    private const string InputActionsPath = "Assets/Input system/Character Control.inputactions";
    private const string LookReferencePath = "Assets/Input system/LookActionReference.asset";
    private const string DustPrefabPath = "Assets/Prefabs/RunDustPuff.prefab";
    private const string DustMaterialPath = "Assets/Prefabs/RunDustMaterial.mat";

    [MenuItem("Lab 4/Refresh Dust Effect Prefab")]
    private static void RefreshDustEffectPrefab()
    {
        string folder = Path.GetDirectoryName(DustPrefabPath);
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        EnsureDustMaterial();
        EnsureDustPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("Run dust prefab refreshed with upward, higher-contrast particles.");
    }

    [MenuItem("Lab 4/Finish Camera and Dust Setup")]
    private static void FinishCameraAndDustSetup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Open the Lab 4 SampleScene before running setup.");
            return;
        }

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("Could not find the Jammo player with Animator and CharacterController in the active scene.");
            return;
        }
        Animator animator = player.GetComponent<Animator>();
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null) head = animator.GetBoneTransform(HumanBodyBones.Spine);
        if (head == null)
        {
            Debug.LogError("The Jammo Animator has no Head or Spine humanoid bone. Check its Avatar is Humanoid.", player);
            return;
        }

        CinemachineCamera virtualCamera = FindVirtualCamera();
        if (virtualCamera == null)
        {
            Debug.LogError("Could not find the existing Cinemachine Camera. Keep the camera object named 'FreeLook Camera' in the active scene.");
            return;
        }
        Camera mainCamera = FindMainCamera();
        if (mainCamera == null)
        {
            Debug.LogError("Could not find a Camera tagged MainCamera in the active scene.");
            return;
        }

        Undo.RecordObject(virtualCamera, "Configure Lab 4 Cinemachine camera");
        CameraTarget target = virtualCamera.Target;
        target.TrackingTarget = head;
        target.LookAtTarget = null;
        target.CustomLookAtTarget = false;
        virtualCamera.Target = target;

        CinemachineOrbitalFollow orbital = virtualCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbital == null) orbital = Undo.AddComponent<CinemachineOrbitalFollow>(virtualCamera.gameObject);
        Undo.RecordObject(orbital, "Set orbital damping");
        orbital.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        orbital.TrackerSettings.PositionDamping = new Vector3(0.55f, 0.7f, 0.55f);
        orbital.TargetOffset = Vector3.zero;

        CinemachineRotationComposer composer = virtualCamera.GetComponent<CinemachineRotationComposer>();
        if (composer == null) composer = Undo.AddComponent<CinemachineRotationComposer>(virtualCamera.gameObject);
        Undo.RecordObject(composer, "Set camera dead zone and aim damping");
        composer.Damping = new Vector2(0.5f, 0.5f);
        ScreenComposerSettings composition = composer.Composition;
        composition.DeadZone = new ScreenComposerSettings.DeadZoneSettings
        { Enabled = true, Size = new Vector2(0.12f, 0.08f) };
        composition.HardLimits = new ScreenComposerSettings.HardLimitSettings
        { Enabled = false, Size = new Vector2(0.8f, 0.8f), Offset = Vector2.zero };
        composer.Composition = composition;

        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null) brain = Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);
        brain.enabled = true;
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
        foreach (CinemachineBrain other in Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
        {
            if (other != brain && other.gameObject.scene == scene && other.enabled)
            {
                Undo.RecordObject(other, "Disable misplaced duplicate Cinemachine Brain");
                other.enabled = false;
                Debug.LogWarning("Disabled a Cinemachine Brain that was not attached to Main Camera.", other);
            }
        }

        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        InputAction lookAction = actions != null ? actions.FindAction("PLAYER/Look", false) : null;
        if (lookAction == null)
        {
            Debug.LogError("Could not find the PLAYER/Look action in Character Control.inputactions.");
            return;
        }
        InputActionReference lookReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(LookReferencePath);
        if (lookReference == null)
        {
            lookReference = InputActionReference.Create(lookAction);
            AssetDatabase.CreateAsset(lookReference, LookReferencePath);
            AssetDatabase.SaveAssets();
        }

        CinemachineInputAxisController axisController = virtualCamera.GetComponent<CinemachineInputAxisController>();
        if (axisController == null) axisController = Undo.AddComponent<CinemachineInputAxisController>(virtualCamera.gameObject);
        axisController.enabled = true;
        axisController.PlayerIndex = -1;
        axisController.AutoEnableInputs = true;
        axisController.SynchronizeControllers();
        Undo.RecordObject(axisController, "Connect Look input to Cinemachine orbital axes");
        ConfigureAxis(axisController, "Look Orbit X", lookReference);
        ConfigureAxis(axisController, "Look Orbit Y", lookReference);
        var zoom = axisController.GetController("Orbit Scale");
        if (zoom != null) zoom.Enabled = false;
        CinemachineLookInputTuner tuner = virtualCamera.GetComponent<CinemachineLookInputTuner>();
        if (tuner == null) tuner = Undo.AddComponent<CinemachineLookInputTuner>(virtualCamera.gameObject);
        tuner.enabled = true;

        string folder = Path.GetDirectoryName(DustPrefabPath);
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        EnsureDustMaterial();
        EnsureDustPrefab();

        FootstepDustEmitter emitter = player.GetComponent<FootstepDustEmitter>();
        if (emitter == null) emitter = Undo.AddComponent<FootstepDustEmitter>(player);
        Undo.RecordObject(emitter, "Configure run dust effect");
        SerializedObject serializedEmitter = new SerializedObject(emitter);
        serializedEmitter.FindProperty("dustPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ParticleSystem>(DustPrefabPath);
        serializedEmitter.FindProperty("leftFoot").objectReferenceValue = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        serializedEmitter.FindProperty("rightFoot").objectReferenceValue = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        serializedEmitter.FindProperty("speedThreshold").floatValue = 5f;
        serializedEmitter.FindProperty("spawnInterval").floatValue = 0.24f;
        serializedEmitter.ApplyModifiedProperties();

        EditorUtility.SetDirty(virtualCamera);
        EditorUtility.SetDirty(orbital);
        EditorUtility.SetDirty(composer);
        EditorUtility.SetDirty(brain);
        EditorUtility.SetDirty(axisController);
        EditorUtility.SetDirty(emitter);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Lab 4 setup complete: Main Camera Brain, Head target, orbital input, and run-only dust prefab are configured.", player);
    }

    private static void ConfigureAxis(CinemachineInputAxisController controller, string name, InputActionReference lookReference)
    {
        var axis = controller.GetController(name);
        if (axis == null)
        {
            Debug.LogError("Cinemachine axis not found: " + name, controller);
            return;
        }
        axis.Enabled = true;
        axis.Input.InputAction = lookReference;
        axis.Input.Gain = 1f;
        axis.Input.CancelDeltaTime = false;
        axis.Driver = new DefaultInputAxisDriver { AccelTime = 0f, DecelTime = 0f };
    }

    private static GameObject FindPlayer()
    {
        GameObject byName = GameObject.Find("Jammo_LowPoly 1");
        if (byName != null && byName.GetComponent<Animator>() != null && byName.GetComponent<CharacterController>() != null)
            return byName;
        foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            if (a.GetComponent<CharacterController>() != null) return a.gameObject;
        return null;
    }

    private static CinemachineCamera FindVirtualCamera()
    {
        foreach (CinemachineCamera c in Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            if (c.gameObject.scene == SceneManager.GetActiveScene() && c.gameObject.name == "FreeLook Camera") return c;
        foreach (CinemachineCamera c in Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            if (c.gameObject.scene == SceneManager.GetActiveScene()) return c;
        return null;
    }

    private static Camera FindMainCamera()
    {
        GameObject byName = GameObject.Find("Main Camera");
        if (byName != null)
        {
            Camera namedCamera = byName.GetComponent<Camera>();
            if (namedCamera != null) return namedCamera;
        }
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c.CompareTag("MainCamera")) continue;
            if (!c.gameObject.activeSelf) c.gameObject.SetActive(true);
            c.enabled = true;
            return c;
        }
        Debug.LogError("No Camera component tagged MainCamera was found. Check the scene's Main Camera object.");
        return null;
    }

    private static void EnsureDustMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "Run Dust Material" };
            AssetDatabase.CreateAsset(material, DustMaterialPath);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
    }

    private static void EnsureDustPrefab()
    {
        GameObject puffObject = new GameObject("Run Dust Puff");
        ParticleSystem system = puffObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startColor = new Color(0.32f, 0.3f, 0.27f, 0.9f);
        main.gravityModifier = -0.08f;
        main.maxParticles = 48;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)14) });
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        shape.randomDirectionAmount = 0.35f;
        var color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.36f, 0.34f, 0.31f), 0f), new GradientColorKey(new Color(0.5f, 0.48f, 0.45f), 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.65f, 0.45f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(gradient);
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 1.35f)));
        ParticleSystemRenderer renderer = puffObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        PrefabUtility.SaveAsPrefabAsset(puffObject, DustPrefabPath);
        Object.DestroyImmediate(puffObject);
    }
}
