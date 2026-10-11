using UnityEngine;

/// <summary>Emits short-lived dust puffs only while the CharacterController is running on the ground.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class FootstepDustEmitter : MonoBehaviour
{
    [Header("Dust Prefab and Feet")]
    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;

    [Header("Run Detection")]
    [Tooltip("Set above walk speed (4) and below sprint speed (6) so dust appears only while running.")]
    [SerializeField, Min(0f)] private float speedThreshold = 5f;
    [SerializeField, Min(0.05f)] private float spawnInterval = 0.24f;
    [SerializeField] private float groundOffset = 0.025f;

    private CharacterController characterController;
    private Animator animator;
    private float nextSpawnTime;
    private bool useLeftFoot = true;
    private Vector3 previousPosition;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
        if (animator != null)
        {
            if (leftFoot == null) leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            if (rightFoot == null) rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }

    // Walk_N includes an OnFootstep animation event; receive it to prevent missing-receiver errors.
    public void OnFootstep() { }

    // JumpLand contains an OnLand animation event; receive it to avoid a missing receiver error.
    public void OnLand() { }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 frameVelocity = (currentPosition - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        previousPosition = currentPosition;
        frameVelocity.y = 0f;

        if (dustPrefab == null || characterController == null)
            return;

        // Use achieved root motion, rather than CharacterController.velocity, so
        // the threshold reflects the movement the player actually experienced.
        if (frameVelocity.sqrMagnitude <= speedThreshold * speedThreshold || Time.time < nextSpawnTime)
            return;

        Transform foot = useLeftFoot ? leftFoot : rightFoot;
        useLeftFoot = !useLeftFoot;
        Vector3 position = foot != null ? foot.position : characterController.bounds.center;
        Bounds controllerBounds = characterController.bounds;
        Vector3 rayOrigin = new Vector3(position.x, controllerBounds.min.y + 0.3f, position.z);
        const float rayDistance = 0.65f;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, rayDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // Never emit while airborne or when no walkable surface is underfoot.
            return;
        }

        float footGap = controllerBounds.min.y - groundHit.point.y;
        if (!characterController.isGrounded && (footGap < -0.1f || footGap > 0.15f))
            return;

        position = groundHit.point + Vector3.up * groundOffset;

        ParticleSystem puff = Instantiate(dustPrefab, position, Quaternion.identity);
        puff.Play(true);
        nextSpawnTime = Time.time + spawnInterval;
    }
}

