using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public sealed class ArtworkInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [SerializeField] private LayerMask artworkLayer;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool logBlockedAttempts;

    [Header("Successful pickup audio")]
    [SerializeField] private AudioClip pickupSuccessSound;
    [SerializeField, Range(0f, 1f)] private float pickupSoundVolume = 0.8f;

    private PlayerInput playerInput;
    private InputAction interactAction;
    private bool initialized;
    private bool subscribed;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null)
            Debug.LogError("ArtworkInteractor needs the active view camera assigned or tagged MainCamera.", this);
    }

    private void Start()
    {
        // Resolve the action after PlayerInput creates its runtime action copy.
        if (playerInput != null && playerInput.actions != null)
            interactAction = playerInput.actions.FindAction("PLAYER/Interact", false);

        if (interactAction == null)
            Debug.LogError("ArtworkInteractor needs PLAYER/Interact in the PlayerInput Actions asset.", this);

        initialized = true;
        SubscribeToInteract();
    }

    private void OnEnable()
    {
        if (initialized)
            SubscribeToInteract();
    }

    private void OnDisable()
    {
        if (subscribed && interactAction != null)
        {
            interactAction.performed -= HandleInteract;
            subscribed = false;
        }
    }

    private void SubscribeToInteract()
    {
        if (!subscribed && interactAction != null)
        {
            interactAction.performed += HandleInteract;
            subscribed = true;
        }
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
            TryDetectArtwork();
    }

    private void TryDetectArtwork()
    {
        if (viewCamera == null)
            return;

        Ray ray = CreateViewRay();
        RaycastHit[] hits = Physics.RaycastAll(ray, GetRayLength(ray), ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            float playerDistance = Vector3.Distance(transform.position, hit.point);
            if (playerDistance > interactionRange)
            {
                if (logBlockedAttempts)
                    Debug.Log($"Artwork out of range: {hit.collider.gameObject.name}", this);
                return;
            }

            int hitLayerMask = 1 << hit.collider.gameObject.layer;
            if ((artworkLayer.value & hitLayerMask) != 0)
            {
                ArtworkWeightPickup weightPickup = hit.collider.GetComponentInParent<ArtworkWeightPickup>();
                if (weightPickup == null)
                {
                    Debug.LogError("Detected artwork has no ArtworkWeightPickup component; pickup was not completed.", hit.collider.gameObject);
                    return;
                }

                if (!weightPickup.TryCollect())
                    return;

                PlayPickupSound();
                Debug.Log($"Detected artwork: {hit.collider.gameObject.name}", hit.collider.gameObject);
                ArtworkDissolve dissolve = hit.collider.GetComponentInParent<ArtworkDissolve>();
                if (dissolve != null)
                    dissolve.BeginDissolve();
                else
                    Debug.LogWarning("Detected artwork has no ArtworkDissolve component.", hit.collider.gameObject);
            }
            else if (logBlockedAttempts)
                Debug.Log($"Interaction blocked by: {hit.collider.gameObject.name}", hit.collider.gameObject);

            // The nearest non-player collider decides the result, so walls occlude targets.
            return;
        }

        if (logBlockedAttempts)
            Debug.Log("No collider hit by the interaction ray.", this);
    }

    private Ray CreateViewRay()
    {
        return new Ray(viewCamera.transform.position, viewCamera.transform.forward);
    }

    private float GetRayLength(Ray ray)
    {
        // Include the camera-to-player offset while enforcing range from the player.
        return interactionRange + Vector3.Distance(transform.position, ray.origin);
    }

    private void PlayPickupSound()
    {
        if (pickupSuccessSound != null)
            AudioSource.PlayClipAtPoint(pickupSuccessSound, transform.position, pickupSoundVolume);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Camera gizmoCamera = viewCamera != null ? viewCamera : Camera.main;
        if (gizmoCamera == null)
            return;

        Ray ray = new Ray(gizmoCamera.transform.position, gizmoCamera.transform.forward);
        float rayLength = interactionRange + Vector3.Distance(transform.position, ray.origin);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * rayLength);
        Gizmos.DrawWireSphere(ray.origin + ray.direction * rayLength, 0.08f);
    }
}
