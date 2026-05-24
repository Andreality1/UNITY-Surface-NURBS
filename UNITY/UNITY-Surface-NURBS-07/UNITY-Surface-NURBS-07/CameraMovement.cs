using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float shiftMultiplier = 2f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Locks the cursor to the center of the screen and hides it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotations based on current camera orientation
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        rotationY = currentRotation.y;
        rotationX = currentRotation.x;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();

        // Optional: Press Escape to unlock the mouse cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleLook()
    {
        // Get mouse input multiplied by sensitivity
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationY += mouseX;
        rotationX -= mouseY; // Inverted because moving mouse up decreases X rotation

        // Clamp the vertical look so you don't flip upside down
        rotationX = Mathf.Clamp(rotationX, minPitch, maxPitch);

        // Apply rotations
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    private void HandleMovement()
    {
        // Get standard WASD/Arrow inputs
// Change "GetAxis" to "GetAxisRaw"
float horizontal = Input.GetAxisRaw("Horizontal"); 
float vertical = Input.GetAxisRaw("Vertical");

        // Calculate direct direction based on where the camera is facing
        Vector3 moveDirection = (transform.forward * vertical) + (transform.right * horizontal);

        // Optional: Add vertical rise/fall (E to go up, Q to go down)
        if (Input.GetKey(KeyCode.E)) moveDirection += transform.up;
        if (Input.GetKey(KeyCode.Q)) moveDirection -= transform.up;

        // Apply Shift speed boost
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= shiftMultiplier;
        }

        // Move the camera frame-rate independently
        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
    }
}