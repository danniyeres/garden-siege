using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private float damagePerShot = 15f;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float range = 3f;
    [SerializeField] private bool turnToTarget = true;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBoolParameter = "Attack";

    private float nextShotTime;
    private bool hasAttackBoolParameter;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        CacheAnimatorParams();
    }

    private void OnDisable()
    {
        SetAttackAnimation(false);
    }

    private void Update()
    {
        var shootHeld = ReadShootInputHeld();
        SetAttackAnimation(shootHeld);

        if (!shootHeld)
        {
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        ShootNearestEnemy();
    }

    private void CacheAnimatorParams()
    {
        if (animator == null || string.IsNullOrEmpty(attackBoolParameter))
        {
            hasAttackBoolParameter = false;
            return;
        }

        var parameters = animator.parameters;
        hasAttackBoolParameter = false;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type != AnimatorControllerParameterType.Bool)
            {
                continue;
            }

            if (parameters[i].name == attackBoolParameter)
            {
                hasAttackBoolParameter = true;
                break;
            }
        }
    }

    private void SetAttackAnimation(bool isAttacking)
    {
        if (animator == null || !hasAttackBoolParameter)
        {
            return;
        }

        animator.SetBool(attackBoolParameter, isAttacking);
    }

    private void ShootNearestEnemy()
    {
        var target = FindNearestEnemyInRange();
        if (target == null)
        {
            return;
        }

        var targetHealth = target.GetComponent<Health>();
        if (targetHealth == null || !targetHealth.IsAlive)
        {
            return;
        }

        targetHealth.ApplyDamage(damagePerShot);

        GameAudioController.Instance.PlayShoot();

        if (turnToTarget)
        {
            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            }
        }
    }

    private EnemyMoveToCrops FindNearestEnemyInRange()
    {
        EnemyMoveToCrops bestEnemy = null;
        var bestDistanceSqr = range * range;
        var origin = transform.position;

        var enemies = FindObjectsByType<EnemyMoveToCrops>(FindObjectsSortMode.None);
        for (var i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            var health = enemy.GetComponent<Health>();
            if (health == null || !health.IsAlive)
            {
                continue;
            }

            var distanceSqr = (enemy.transform.position - origin).sqrMagnitude;
            if (distanceSqr > bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            bestEnemy = enemy;
        }

        return bestEnemy;
    }

    private static bool ReadShootInputHeld()
    {
        var pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            pressed = true;
        }

        if (Keyboard.current != null && Keyboard.current.fKey.isPressed)
        {
            pressed = true;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            pressed = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Space))
        {
            pressed = true;
        }
#endif

        return pressed;
    }
}
