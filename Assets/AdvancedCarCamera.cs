using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AdvancedCarCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Base Settings")]
    public float baseDistance = 6f;
    public float baseHeight = 3f;

    [Header("Speed-Based Adjustment")]
    public float speedMultiplier = 0.2f;
    public float maxExtraDistance = 6f;
    public float maxExtraHeight = 2f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.2f;
    public float rotationSmoothTime = 5f;

    [Header("Look At Settings")]
    public bool smoothLookAt = true;
    public float lookAtSmoothSpeed = 5f;

    [Header("Tilt Settings")]
    public bool enableTilt = true;
    public float maxSideTiltAngle = 10f;    // Left/right
    public float maxForwardTiltAngle = 8f;  // Acceleration/braking
    public float tiltSmoothSpeed = 5f;

    private Vector3 currentVelocity;
    private Rigidbody targetRb;

    private float currentSideTilt = 0f;
    private float currentForwardTilt = 0f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("AdvancedCarCamera: No target assigned!");
            enabled = false;
            return;
        }

        targetRb = target.GetComponent<Rigidbody>();
        if (targetRb == null)
        {
            Debug.LogWarning("AdvancedCarCamera: Target has no Rigidbody. Speed-based features will not work.");
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        float speed = targetRb ? targetRb.velocity.magnitude : 0f;

        // Distance & height adjustment
        float dynamicDistance = baseDistance + Mathf.Min(speed * speedMultiplier, maxExtraDistance);
        float dynamicHeight = baseHeight + Mathf.Min(speed * speedMultiplier, maxExtraHeight);

        Vector3 targetOffset = -target.forward * dynamicDistance + Vector3.up * dynamicHeight;
        Vector3 desiredPosition = target.position + targetOffset;

        // Smooth position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

        // Base look rotation
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);

        if (enableTilt && targetRb)
        {
            Vector3 localVelocity = target.InverseTransformDirection(targetRb.velocity);

            // 🔄 Side tilt (left/right)
            float sideTilt = Mathf.Clamp(localVelocity.x, -1f, 1f);
            float targetSideTilt = -sideTilt * maxSideTiltAngle;
            currentSideTilt = Mathf.Lerp(currentSideTilt, targetSideTilt, Time.deltaTime * tiltSmoothSpeed);

            // ⬆️⬇️ Forward tilt (acceleration/braking)
            float forwardTilt = Mathf.Clamp(localVelocity.z / 10f, -1f, 1f);  // normalize
            float targetForwardTilt = -forwardTilt * maxForwardTiltAngle;
            currentForwardTilt = Mathf.Lerp(currentForwardTilt, targetForwardTilt, Time.deltaTime * tiltSmoothSpeed);

            // Apply tilt (X = forward, Z = side)
            targetRotation *= Quaternion.Euler(currentForwardTilt, 0, currentSideTilt);
        }

        // Smooth rotation
        if (smoothLookAt)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lookAtSmoothSpeed);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
}
