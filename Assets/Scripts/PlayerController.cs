using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private bool cameraRelativeMovement = true;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private bool disableLegacyAnimationDriver = true;
    [SerializeField] private bool useStrafeAnimations = false;

    private CharacterController characterController;
    private float verticalVelocity;
    private bool hasSpeedParam;
    private bool hasRunParam;
    private bool hasRunLeftParam;
    private bool hasRunRightParam;
    private bool hasRunBackParam;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (disableLegacyAnimationDriver)
        {
            DisableLegacyAnimationDriver();
        }

        CacheAnimatorParams();
    }

    private void Update()
    {
        var input = ReadMoveInput();
        var moveDirection = BuildMoveDirection(input);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        var velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        RotateToward(moveDirection);
        UpdateAnimator(input, moveDirection);
    }

    private Vector3 BuildMoveDirection(Vector2 input)
    {
        var direction = new Vector3(input.x, 0f, input.y);

        if (!cameraRelativeMovement || cameraTransform == null)
        {
            return Vector3.ClampMagnitude(direction, 1f);
        }

        var forward = cameraTransform.forward;
        var right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        var cameraRelativeDirection = forward * direction.z + right * direction.x;
        return Vector3.ClampMagnitude(cameraRelativeDirection, 1f);
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void DisableLegacyAnimationDriver()
    {
        var behaviours = GetComponents<MonoBehaviour>();
        for (var i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled)
            {
                continue;
            }

            if (behaviour.GetType().Name == "ChaScript")
            {
                behaviour.enabled = false;
            }
        }
    }

    private void UpdateAnimator(Vector2 input, Vector3 moveDirection)
    {
        if (animator == null)
        {
            return;
        }

        if (hasSpeedParam)
        {
            animator.SetFloat("Speed", moveDirection.magnitude);
        }

        var isMoving = moveDirection.sqrMagnitude > 0.0001f;
        var isBack = input.y < -0.01f;

        if (hasRunParam)
        {
            animator.SetBool("Run", isMoving && !isBack);
        }

        if (hasRunBackParam)
        {
            animator.SetBool("RunBack", isBack);
        }

        if (hasRunLeftParam)
        {
            animator.SetBool("RunLeft", useStrafeAnimations && input.x < -0.01f);
        }

        if (hasRunRightParam)
        {
            animator.SetBool("RunRight", useStrafeAnimations && input.x > 0.01f);
        }
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.aKey.isPressed)
            {
                x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                x += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                y += 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            1f
        );
#else
        return Vector2.zero;
#endif
    }

    private void CacheAnimatorParams()
    {
        if (animator == null)
        {
            return;
        }

        var parameters = animator.parameters;
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterName = parameters[i].name;
            if (parameterName == "Speed")
            {
                hasSpeedParam = true;
            }
            else if (parameterName == "Run")
            {
                hasRunParam = true;
            }
            else if (parameterName == "RunLeft")
            {
                hasRunLeftParam = true;
            }
            else if (parameterName == "RunRight")
            {
                hasRunRightParam = true;
            }
            else if (parameterName == "RunBack")
            {
                hasRunBackParam = true;
            }
        }
    }
}
