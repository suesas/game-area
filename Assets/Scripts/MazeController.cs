using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MazeController : MonoBehaviour
{
    [Tooltip("The speed at which the maze rotates.")]
    [SerializeField] private float rotationSpeed = 20f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Input should be read in Update for responsiveness
    }

    void FixedUpdate()
    {
        // Get input from horizontal and vertical axes (Arrow keys or WASD)
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate rotation amounts. 
        // We rotate around the Z-axis for horizontal input and X-axis for vertical input.
        float zRotation = horizontalInput * rotationSpeed * Time.deltaTime;
        float xRotation = -verticalInput * rotationSpeed * Time.deltaTime; // Negative to match intuitive control

        // Create a quaternion for the rotation delta
        Quaternion deltaRotation = Quaternion.Euler(xRotation, 0, zRotation);

        // Apply the rotation to the Rigidbody. This is the correct way to rotate a kinematic Rigidbody
        // to ensure proper interaction with other physics objects.
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}
