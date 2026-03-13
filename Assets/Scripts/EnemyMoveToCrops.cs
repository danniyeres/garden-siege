using UnityEngine;

public class EnemyMoveToCrops : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform cropsRoot;
    [SerializeField] private bool targetNearestChild = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float damagePerSecond = 5f;
    [SerializeField] private bool useCharacterController = true;
    [SerializeField] private float gravity = -20f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private bool obstacleAvoidance = true;
    [SerializeField] private float obstacleProbeDistance = 1.1f;
    [SerializeField] private float obstacleProbeRadius = 0.35f;
    [SerializeField] private LayerMask obstacleMask = -1;

    private Transform target;
    private Health cropsHealth;
    private CharacterController characterController;
    private float verticalVelocity;
    private float lastAvoidSign = 1f;
    private float attackSoundCooldown;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (useCharacterController && characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1f;
            characterController.radius = 0.45f;
            characterController.center = new Vector3(0f, 0.5f, 0f);
            characterController.stepOffset = 0.25f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0.001f;
        }

        if (cropsRoot == null)
        {
            var crops = GameObject.Find("Crops");
            if (crops != null)
            {
                cropsRoot = crops.transform;
            }
        }

        ResolveCropsHealth();
        ResolveTarget();
    }

    private void Update()
    {
        if (attackSoundCooldown > 0f)
        {
            attackSoundCooldown -= Time.deltaTime;
        }

        if (target == null)
        {
            ResolveTarget();
            if (target == null)
            {
                return;
            }
        }

        var toTarget = target.position - transform.position;
        toTarget.y = 0f;

        var distance = toTarget.magnitude;
        if (distance <= stopDistance)
        {
            if (cropsHealth != null && cropsHealth.IsAlive)
            {
                cropsHealth.ApplyDamage(damagePerSecond * Time.deltaTime);

                if (attackSoundCooldown <= 0f)
                {
                    GameAudioController.Instance.PlayEnemyAttack();
                    attackSoundCooldown = Random.Range(0.45f, 0.85f);
                }
            }

            return;
        }

        var moveDirection = toTarget / distance;
        moveDirection = ComputeSteeredDirection(moveDirection);
        if (useCharacterController && characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            var velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        var desiredRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void SetCropsRoot(Transform newCropsRoot)
    {
        cropsRoot = newCropsRoot;
        ResolveCropsHealth();
        ResolveTarget();
    }

    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
    }

    public void SetDamagePerSecond(float newDamagePerSecond)
    {
        damagePerSecond = Mathf.Max(0.1f, newDamagePerSecond);
    }

    private void ResolveTarget()
    {
        target = null;
        if (cropsRoot == null)
        {
            return;
        }

        if (!targetNearestChild || cropsRoot.childCount == 0)
        {
            target = cropsRoot;
            return;
        }

        var bestDistance = float.MaxValue;
        var currentPosition = transform.position;
        for (var i = 0; i < cropsRoot.childCount; i++)
        {
            var child = cropsRoot.GetChild(i);
            var sqrDistance = (child.position - currentPosition).sqrMagnitude;
            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                target = child;
            }
        }

        if (target == null)
        {
            target = cropsRoot;
        }
    }

    private Vector3 ComputeSteeredDirection(Vector3 desiredDirection)
    {
        if (!obstacleAvoidance)
        {
            return desiredDirection;
        }

        if (!IsDirectionBlocked(desiredDirection))
        {
            return desiredDirection;
        }

        var testAngles = new[] { 30f, 50f, 70f, 95f, 125f, 155f };
        var firstSign = lastAvoidSign >= 0f ? 1f : -1f;
        var secondSign = -firstSign;

        for (var i = 0; i < testAngles.Length; i++)
        {
            var angle = testAngles[i];
            var firstCandidate = Quaternion.Euler(0f, angle * firstSign, 0f) * desiredDirection;
            if (!IsDirectionBlocked(firstCandidate))
            {
                lastAvoidSign = firstSign;
                return firstCandidate.normalized;
            }

            var secondCandidate = Quaternion.Euler(0f, angle * secondSign, 0f) * desiredDirection;
            if (!IsDirectionBlocked(secondCandidate))
            {
                lastAvoidSign = secondSign;
                return secondCandidate.normalized;
            }
        }

        return desiredDirection;
    }

    private bool IsDirectionBlocked(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        var castDirection = direction.normalized;
        var castOrigin = transform.position + Vector3.up * 0.5f;
        if (
            !Physics.SphereCast(
                castOrigin,
                obstacleProbeRadius,
                castDirection,
                out var hit,
                obstacleProbeDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            return false;
        }

        var hitTransform = hit.transform;
        if (hitTransform == null)
        {
            return false;
        }

        if (hitTransform == transform || hitTransform.IsChildOf(transform))
        {
            return false;
        }

        if (target != null && (hitTransform == target || hitTransform.IsChildOf(target)))
        {
            return false;
        }

        if (cropsRoot != null && (hitTransform == cropsRoot || hitTransform.IsChildOf(cropsRoot)))
        {
            return false;
        }

        return true;
    }

    private void ResolveCropsHealth()
    {
        cropsHealth = null;
        if (cropsRoot == null)
        {
            return;
        }

        cropsHealth = cropsRoot.GetComponent<Health>();
        if (cropsHealth == null)
        {
            cropsHealth = cropsRoot.GetComponentInParent<Health>();
        }
    }
}
