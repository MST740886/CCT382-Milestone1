using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Adds a small Cinemachine impulse when the character lands after being airborne.
/// Bootstrapped for the existing SampleScene so no second movement controller is needed.
/// </summary>
internal sealed class LandingCameraImpulse : MonoBehaviour
{
    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource;
    private bool wasGrounded;
    private float strongestDownwardSpeed;

    private const float MinimumLandingSpeed = 1.5f;
    private const float ImpulseStrength = 0.16f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForLoadedScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (SceneManager.GetActiveScene().name == "SampleScene")
            AttachToSceneObjects();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
            AttachToSceneObjects();
    }

    private static void AttachToSceneObjects()
    {
        var character = Object.FindFirstObjectByType<CharacterController>();
        if (character != null && character.GetComponent<LandingCameraImpulse>() == null)
            character.gameObject.AddComponent<LandingCameraImpulse>();

        var camera = Object.FindFirstObjectByType<CinemachineCamera>();
        if (camera != null && camera.GetComponent<CinemachineImpulseListener>() == null)
            camera.gameObject.AddComponent<CinemachineImpulseListener>();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();

        wasGrounded = characterController.isGrounded;
    }

    private void Update()
    {
        if (characterController == null)
            return;

        bool grounded = characterController.isGrounded;
        if (!grounded)
            strongestDownwardSpeed = Mathf.Max(strongestDownwardSpeed, -characterController.velocity.y);

        if (grounded && !wasGrounded && strongestDownwardSpeed >= MinimumLandingSpeed)
        {
            float strength = ImpulseStrength * Mathf.Clamp(strongestDownwardSpeed / 5f, 0.7f, 1.5f);
            impulseSource.GenerateImpulseWithVelocity(Vector3.down * strength);
            strongestDownwardSpeed = 0f;
        }

        wasGrounded = grounded;
    }
}
