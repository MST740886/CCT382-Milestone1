using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves Jammo from the existing Starter Assets Send Messages input component
/// and drives the MoveX/MoveY locomotion Blend Tree plus jump triggers.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float sprintSpeed = 6f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField, Min(0.01f)] private float jumpBufferTime = 0.15f;
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.18f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;

    [Header("Blend Tree")]
    [SerializeField, Min(0f)] private float animationDampTime = 0.08f;
    private static readonly int BackstepActiveHash =
    Animator.StringToHash("BackstepActive");

    [SerializeField, Min(0.01f)] private float backstepDistance = 0.8f;
    [SerializeField, Min(0.01f)] private float backstepDuration = 0.35f;

    private InputAction backstepAction;
    private bool backstepRequested;
    private float backstepTimeRemaining;
    private Vector3 backstepDirection;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int JumpStartHash = Animator.StringToHash("JumpStart");
    private static readonly int JumpLandHash = Animator.StringToHash("JumpLand");
    private static readonly string InputsTypeName = "StarterAssets.StarterAssetsInputs";

    private CharacterController characterController;
    private Transform cameraTransform;
    private Animator animator;
    private PlayerInput playerInput;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private Component starterInputs;
    private FieldInfo moveField;
    private float verticalVelocity;
    private float jumpBufferRemaining;
    private bool jumpRequested;
    private bool wasAirborne;
    private bool hasEstablishedGroundedState;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            sprintAction = playerInput.actions.FindAction("PLAYER/Sprint", false);
            jumpAction = playerInput.actions.FindAction("PLAYER/Jump", false);
        }
        
        FindStarterAssetsInputs();
        if (playerInput == null || sprintAction == null)
            Debug.LogError("MovementInput needs PlayerInput with a PLAYER/Sprint action.", this);
        if (jumpAction == null)
            Debug.LogError("MovementInput needs PlayerInput with a PLAYER/Jump action.", this);
        if (animator != null && !HasTrigger(JumpStartHash))
            Debug.LogWarning("Animator needs a Trigger parameter named JumpStart for the jump start animation.", this);
        if (animator != null && !HasTrigger(JumpLandHash))
            Debug.LogWarning("Animator needs a Trigger parameter named JumpLand for the jump land animation.", this);
        
        backstepAction = playerInput.actions.FindAction("PLAYER/Backstep", false);

        if (backstepAction == null)
        Debug.LogError("MovementInput needs the PLAYER/Backstep action.", this);
    
    }

    private void OnEnable()
    {
        if (jumpAction != null)
            jumpAction.performed += QueueJump;
        if (backstepAction != null)
            backstepAction.performed += QueueBackstep;
    }

    private void OnDisable()
    {
        if (jumpAction != null)
            jumpAction.performed -= QueueJump;
        if (backstepAction != null)
            backstepAction.performed -= QueueBackstep;
    }

    private void QueueJump(InputAction.CallbackContext context)
    {
        jumpRequested = true;
    }
    private void QueueBackstep(InputAction.CallbackContext context)
    {
        backstepRequested = true;
    }

    private void Update()
    {
        if (characterController == null || animator == null || starterInputs == null || moveField == null)
            return;

        Vector2 input = (Vector2)moveField.GetValue(starterInputs);
        input = Vector2.ClampMagnitude(input, 1f);
        bool isMoving = input.sqrMagnitude > 0.001f;
        bool isSprinting = isMoving && sprintAction != null && sprintAction.IsPressed();
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (backstepRequested)
        {
            backstepRequested = false;

            if (backstepTimeRemaining <= 0f &&
                IsGrounded() &&
                jumpBufferRemaining <= 0f)
            {
                backstepDirection = -transform.forward;
                backstepTimeRemaining = backstepDuration;
                animator.SetBool(BackstepActiveHash, true);
            }
        }

        bool isBackstepping = backstepTimeRemaining > 0f;

        if (isBackstepping)
        {
            backstepTimeRemaining =
                Mathf.Max(0f, backstepTimeRemaining - Time.deltaTime);

            if (backstepTimeRemaining <= 0f)
                animator.SetBool(BackstepActiveHash, false);
        }

        Vector3 horizontal;

        if (isBackstepping)
        {
            horizontal = backstepDirection;
        }
        else if (cameraTransform != null)
        {
            Vector3 cameraForward =
                Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight =
                Vector3.Cross(Vector3.up, cameraForward).normalized;

            horizontal = cameraRight * input.x + cameraForward * input.y;
        }
        else
        {
            horizontal = new Vector3(input.x, 0f, input.y);
        }

        horizontal = Vector3.ClampMagnitude(horizontal, 1f);

        if (!isBackstepping && horizontal.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontal, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (jumpRequested)
        {
            jumpBufferRemaining = jumpBufferTime;
            jumpRequested = false;
            Debug.Log($"Jump input received. Grounded: {IsGrounded()} (CharacterController: {characterController.isGrounded})", this);
        }
        if (jumpBufferRemaining > 0f)
            jumpBufferRemaining -= Time.deltaTime;

        bool groundedBeforeMove = IsGrounded();
        if (groundedBeforeMove && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (groundedBeforeMove && jumpBufferRemaining > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferRemaining = 0f;
            Debug.Log("Jump accepted by CharacterController.", this);
            if (HasTrigger(JumpStartHash))
                animator.SetTrigger(JumpStartHash);
        }
        else if (!groundedBeforeMove)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        float horizontalSpeed = isBackstepping
        ? backstepDistance / backstepDuration
        : currentSpeed;

        Vector3 velocity = horizontal * horizontalSpeed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        bool groundedAfterMove = IsGrounded();
        if (!groundedAfterMove)
        {
            // Ignore the initial settle onto the floor; only count leaving ground
            // after the character has first stood on it.
            if (hasEstablishedGroundedState)
                wasAirborne = true;
        }
        else
        {
            if (wasAirborne && HasTrigger(JumpLandHash))
            {
                animator.SetTrigger(JumpLandHash);
                Debug.Log($"Landing detected: JumpLand sent. Grounded={groundedAfterMove}, Animator state={animator.GetCurrentAnimatorStateInfo(0).shortNameHash}", this);
            }
            hasEstablishedGroundedState = true;
            wasAirborne = false;
        }

        // The character faces its movement direction, so the available forward
        // clips use MoveY=1 for walk and MoveY=2 for run.
        animator.SetFloat(MoveXHash, 0f, animationDampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, !isMoving ? 0f : isSprinting ? 2f : 1f,
            animationDampTime, Time.deltaTime);
    }

    private bool IsGrounded()
    {
        if (characterController.isGrounded)
            return true;

        // Do not treat the floor beneath the character as grounded while rising.
        // This short ray is a fallback for CharacterController.isGrounded being
        // false on a frame when the capsule is already standing on the floor.
        if (verticalVelocity > 0f)
            return false;

        Bounds bounds = characterController.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundProbeDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider == characterController || hit.collider.transform.IsChildOf(transform))
            return false;

        return Vector3.Angle(hit.normal, Vector3.up) <= characterController.slopeLimit;
    }

    private void FindStarterAssetsInputs()
    {
        // This project contains package and local versions with the same full type name.
        // Prefer the package component, otherwise use the local component updated by Send Messages.
        Component fallback = null;
        FieldInfo fallbackMoveField = null;
        foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
        {
            if (component == null)
                continue;

            System.Type type = component.GetType();
            if (type.FullName != InputsTypeName)
                continue;

            FieldInfo candidateMove = type.GetField("move", BindingFlags.Instance | BindingFlags.Public);
            if (candidateMove == null || candidateMove.FieldType != typeof(Vector2))
                continue;

            if (type.Assembly.GetName().Name == "Unity.StarterAssets")
            {
                starterInputs = component;
                moveField = candidateMove;
                break;
            }

            fallback = component;
            fallbackMoveField = candidateMove;
        }

        if (starterInputs == null)
        {
            starterInputs = fallback;
            moveField = fallbackMoveField;
        }

        if (starterInputs == null || moveField == null)
            Debug.LogError("MovementInput needs a StarterAssetsInputs component with a public Vector2 move field on this GameObject.", this);
        if (animator != null && (!HasFloatParameter(MoveXHash) || !HasFloatParameter(MoveYHash)))
            Debug.LogError("The assigned Animator Controller must contain float parameters MoveX and MoveY.", this);
    }

    private bool HasFloatParameter(int hash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Float)
                return true;
        return false;
    }

    private bool HasTrigger(int hash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Trigger)
                return true;
        return false;
    }
}


