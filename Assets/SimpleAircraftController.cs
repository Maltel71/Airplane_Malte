using UnityEngine;

public class SimpleAircraftController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float throttle = 5f;
    public float maxSpeed = 25f;
    public float liftPower = 50f;
    public float dragFactor = 0.98f;

    [Header("Control Sensitivity")]
    public float pitchSensitivity = 20f;  // Much lower pitch sensitivity
    public float rollSensitivity = 30f;
    public float yawSensitivity = 40f;

    [Header("Auto-Level Settings")]
    public float rollCorrection = 5f;
    public float pitchCorrection = 1f;    // Much weaker pitch correction

    [Header("References")]
    public MonoBehaviour mouseFlightController; // Using MonoBehaviour instead of specific type

    [Header("Manual Controls (Optional)")]
    public bool useKeyboardControls = true;

    private Rigidbody rb;
    private float rollInput;
    private float pitchInput;
    private float yawInput;

    // Mouse flight variables
    private Vector3 lastMouseWorldPos;
    private float mouseRollInput;
    private float mousePitchInput;
    private float mouseYawInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Find MouseFlightController if not assigned
        if (mouseFlightController == null)
        {
            mouseFlightController = FindObjectOfType<MonoBehaviour>();
            // You'll need to manually assign this in the inspector
        }

        // Set initial mouse position
        if (mouseFlightController != null)
        {
            // lastMouseWorldPos = mouseFlightController.MouseAimPos;
            // Commented out until we fix the MouseFlightController reference
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Reset inputs
        rollInput = 0f;
        pitchInput = 0f;
        yawInput = 0f;

        // Keyboard controls (optional)
        if (useKeyboardControls)
        {
            rollInput += -Input.GetAxis("Horizontal"); // A/D or Arrow Keys
            pitchInput += Input.GetAxis("Vertical"); // W/S or Arrow Keys

            if (Input.GetKey(KeyCode.Q)) yawInput -= 1f;
            if (Input.GetKey(KeyCode.E)) yawInput += 1f;
        }

        // Mouse flight controls
        if (mouseFlightController != null)
        {
            // We'll need to use reflection or make this work differently
            // For now, let's disable mouse flight until we fix the reference
            /*
            Vector3 currentMousePos = mouseFlightController.MouseAimPos;
            Vector3 aircraftToMouse = currentMousePos - transform.position;
            
            // Convert world direction to local aircraft space
            Vector3 localDirection = transform.InverseTransformDirection(aircraftToMouse.normalized);
            
            // Calculate mouse-based pitch and yaw inputs
            mousePitchInput = Mathf.Clamp(localDirection.y, -1f, 1f);
            mouseYawInput = Mathf.Clamp(localDirection.x, -1f, 1f);
            
            // Calculate roll based on how fast mouse is moving horizontally
            Vector3 mouseDelta = currentMousePos - lastMouseWorldPos;
            Vector3 localMouseDelta = transform.InverseTransformDirection(mouseDelta);
            mouseRollInput = Mathf.Clamp(localMouseDelta.x * 10f, -1f, 1f);
            
            // Combine mouse inputs with manual inputs
            pitchInput += mousePitchInput * 0.8f;
            yawInput += mouseYawInput * 0.6f;
            rollInput += mouseRollInput * 0.7f;
            
            // Add banking roll when turning
            rollInput += -mouseYawInput * 0.8f;
            
            lastMouseWorldPos = currentMousePos;
            */
        }

        // Clamp all inputs
        rollInput = Mathf.Clamp(rollInput, -1f, 1f);
        pitchInput = Mathf.Clamp(pitchInput, -1f, 1f);
        yawInput = Mathf.Clamp(yawInput, -1f, 1f);
    }

    void FixedUpdate()
    {
        // Apply thrust
        Vector3 thrust = transform.forward * throttle;
        rb.AddForce(thrust);

        // Apply lift (simplified)
        Vector3 lift = Vector3.up * rb.linearVelocity.magnitude * liftPower / 1000f;
        rb.AddForce(lift);

        // Apply drag
        rb.linearVelocity *= dragFactor;

        // Limit max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // Apply rotational forces
        ApplyRotation();

        // Auto-level (optional, helps with stability)
        ApplyAutoLevel();
    }

    void ApplyRotation()
    {
        // Apply pitch (nose up/down)
        Vector3 pitchTorque = transform.right * pitchInput * pitchSensitivity;
        rb.AddTorque(pitchTorque);

        // Apply roll (barrel roll!)
        Vector3 rollTorque = transform.forward * rollInput * rollSensitivity;
        rb.AddTorque(rollTorque);

        // Apply yaw (nose left/right)
        Vector3 yawTorque = transform.up * yawInput * yawSensitivity;
        rb.AddTorque(yawTorque);
    }

    void ApplyAutoLevel()
    {
        // Auto-level roll (keep wings level when not actively rolling)
        if (Mathf.Abs(rollInput) < 0.1f)
        {
            Vector3 levelTorque = Vector3.Cross(transform.up, Vector3.up) * rollCorrection;
            rb.AddTorque(levelTorque);
        }

        // Much gentler auto-level pitch - only prevent extreme nose diving
        if (Mathf.Abs(pitchInput) < 0.05f && transform.forward.y < -0.5f) // Only if nose diving badly
        {
            Vector3 pitchUpTorque = transform.right * pitchCorrection;
            rb.AddTorque(pitchUpTorque);
        }
    }

    void OnDrawGizmos()
    {
        // Draw flight path
        if (mouseFlightController != null)
        {
            /*
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, mouseFlightController.MouseAimPos);
            Gizmos.DrawWireSphere(mouseFlightController.MouseAimPos, 2f);
            */
        }

        // Draw velocity vector
        Gizmos.color = Color.blue;
        if (rb != null)
        {
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 10f);
        }

        // Draw aircraft orientation
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 3f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.up * 3f);
    }
}