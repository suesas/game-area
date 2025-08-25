using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MazeController : MonoBehaviour
{
    [Header("Gimbals")]
    [SerializeField] private Transform outerGimbalZ; // rotates around Z
    [SerializeField] private Transform innerGimbalX; // rotates around X

    [Header("Controls")]
    [Tooltip("Degrees per second for tilt input")]
    [SerializeField] private float rotationSpeed = 20f;
    [Tooltip("Maximum absolute tilt in degrees per axis")]
    [SerializeField] private float maxTiltDegrees = 15f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    // Optional helper called by importer to wire gimbals automatically
    private void ConfigureGimbals(object[] args)
    {
        if (args == null || args.Length < 2) return;
        outerGimbalZ = args[0] as Transform;
        innerGimbalX = args[1] as Transform;
    }

    void FixedUpdate()
    {
        if (outerGimbalZ == null || innerGimbalX == null) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        float zDelta = horizontalInput * rotationSpeed * Time.fixedDeltaTime;
        float xDelta = -verticalInput * rotationSpeed * Time.fixedDeltaTime;

        // Outer (Z)
        float currentZ = NormalizeAngle(outerGimbalZ.localEulerAngles.z);
        float targetZ = Mathf.Clamp(currentZ + zDelta, -maxTiltDegrees, maxTiltDegrees);
        outerGimbalZ.localRotation = Quaternion.Euler(0f, 0f, targetZ);

        // Inner (X)
        float currentX = NormalizeAngle(innerGimbalX.localEulerAngles.x);
        float targetX = Mathf.Clamp(currentX + xDelta, -maxTiltDegrees, maxTiltDegrees);
        innerGimbalX.localRotation = Quaternion.Euler(targetX, 0f, 0f);
    }

    private float NormalizeAngle(float degrees)
    {
        degrees %= 360f;
        if (degrees > 180f) degrees -= 360f;
        return degrees;
    }
}
