using UnityEngine;

[DefaultExecutionOrder(1000)]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool keepInitialOffset = true;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private bool keepInitialRotation = true;

    private Quaternion initialRotation;
    private bool initialized;

    private void Awake()
    {
        initialRotation = transform.rotation;
        TryResolveTarget();
        InitializeOffsetIfNeeded();
    }

    private void LateUpdate()
    {
        TryResolveTarget();
        if (target == null)
        {
            return;
        }

        InitializeOffsetIfNeeded();

        var desiredPosition = target.position + offset;
        if (followSpeed <= 0f)
        {
            transform.position = desiredPosition;
        }
        else
        {
            var t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        }

        if (keepInitialRotation)
        {
            transform.rotation = initialRotation;
        }
    }

    public void SetTarget(Transform newTarget, bool recalculateOffset = true)
    {
        target = newTarget;
        if (target == null)
        {
            initialized = false;
            return;
        }

        if (recalculateOffset)
        {
            offset = transform.position - target.position;
        }

        initialized = true;
    }

    private void TryResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        var byName = GameObject.Find("JellyFishGirl");
        if (byName != null)
        {
            target = byName.transform;
            return;
        }

        var byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag != null)
        {
            target = byTag.transform;
        }
    }

    private void InitializeOffsetIfNeeded()
    {
        if (initialized || target == null)
        {
            return;
        }

        if (keepInitialOffset)
        {
            offset = transform.position - target.position;
        }

        initialized = true;
    }
}
