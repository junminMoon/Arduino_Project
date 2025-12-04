using UnityEngine;
using TMPro; 
using System.Collections; // Required for Coroutines (Reloading)

public class DartThrower : MonoBehaviour
{
    // ==========================================
    // 1. Connection and UI Elements
    // ==========================================
    [Header("Connection Elements")]
    public ArduinoPackage arduinoPackage; 
    public GameObject dartPrefab;      // Dart prefab to launch
    public Transform spawnPoint;       // Dart spawn location (Hand position)
    private FollowCamera followCamera; // FollowCamera reference

    [Header("UI Elements")]
    public TextMeshProUGUI statusText; 

    [Header("Debugging Settings")] 
    public KeyCode debugThrowKey = KeyCode.Space;
    public KeyCode debugGripKey = KeyCode.T;
    public Vector3 debugAccel = new Vector3(3.0f, 0f, 0f); // Acceleration vector for keyboard debugging

    // Internal Dart Management
    private GameObject currentDart;
    private Rigidbody currentRb;

    // ==========================================
    // 2. Throwing and Aiming Settings
    // ==========================================
    [Header("Throwing Settings")]
    public float throwThreshold = 2.0f; // Acceleration threshold (g)
    public float forceMultiplier = 50.0f; 
    public float cooldownTime = 1.0f;

    [Header("Aiming (Tilt) Settings")]
    public float rotationSmoothness = 10f; 
    public Vector3 rotationOffset = new Vector3(0, 0, 0); 
    public bool invertPitch = false; 
    public bool invertRoll = false;

    [Header("Launch Direction Correction")]
    [Tooltip("Check if the dart flies backward to invert the force vector.")]
    public bool invertThrowDirection = false; 
    [Tooltip("World coordinate offset correction for MPU vs World alignment (e.g., Vector3(0, 90, 0) for 90-degree Y-axis error)")]
    public Vector3 WorldLaunchOffset = new Vector3(0, 0, 0); 

    // ==========================================
    // 3. State Variables
    // ==========================================
    private float lastThrowTime;
    private bool isReadyToThrow = true; 
    private bool isGripping = false;    
    private Quaternion m_AimingRotation = Quaternion.identity; // Stores MPU aiming rotation

    void Start()
    {   
        // Get references on startup
        arduinoPackage = FindObjectOfType<ArduinoPackage>(); 
        followCamera = FindObjectOfType<FollowCamera>();
        
        UpdateStatusUI("Dart Ready: Awaiting Grip");
    }

    void Update()
    {
        // 1. Check for debugging mode (ArduinoPackage is null)
        bool isDebugging = (arduinoPackage == null);
        
        // 2. Update Grip State
        UpdateGrippingState(isDebugging); 

        // 3. Aiming and Throw Detection Logic
        if (isGripping && isReadyToThrow)
        {
            Vector3 currentAccel;
            bool shouldThrow;

            // Aiming: Rotate dart based on MPU input
            if (currentDart != null)
            {
                if (!isDebugging)
                {
                    UpdateAiming(currentDart.transform);
                }
            }

            // Determine Acceleration and Throw Condition
            if (isDebugging)
            {
                currentAccel = debugAccel;
                shouldThrow = Input.GetKeyDown(debugThrowKey); // Check for key press
            }
            else
            {
                currentAccel = new Vector3(
                    arduinoPackage.RawAccelX, 
                    arduinoPackage.RawAccelY, 
                    arduinoPackage.RawAccelZ
                );
                // Check if acceleration magnitude exceeds threshold
                shouldThrow = currentAccel.magnitude > throwThreshold;
            }

            // Execute Throw
            if (currentDart != null && shouldThrow && Time.time > lastThrowTime + cooldownTime)
            {
                ThrowDart(currentAccel);
            }
        }
    }

    // ==========================================
    // 4. State Management and UI Logic
    // ==========================================

    private void UpdateStatusUI(string message, Color color = default)
    {
        if (statusText != null)
        {
            statusText.text = message;
            if (color != default)
            {
                statusText.color = color;
            }
        }
    }

    // Handles the grip/release state based on Arduino Touch or Debug Key
    void UpdateGrippingState(bool isDebugging)
    {
        bool touchPressed;
        
        // Input Source determination
        if (isDebugging)
        {
            touchPressed = Input.GetKey(debugGripKey); 
        }
        else
        {
            if (arduinoPackage == null) return; 
            touchPressed = arduinoPackage.IsTouchPressed;
        }

        if (touchPressed && !isGripping && isReadyToThrow)
        {
            // Grip Start
            isGripping = true;
            PrepareDart();
            
            string debugKeyMsg = isDebugging ? $" (Throw Key: {debugThrowKey.ToString()})" : "";
            UpdateStatusUI("🎯 Aiming... (Awaiting Throw)" + debugKeyMsg, Color.green); 
        }
        else if (!touchPressed && isGripping)
        {
            // Grip Release (Cancel Throw)
            isGripping = false;
            if (currentDart != null)
            {
                Destroy(currentDart);
                currentDart = null;
                currentRb = null;
                UpdateStatusUI("Dart Canceled: Awaiting Grip", Color.black);
            }
        }
        else if (!isGripping && isReadyToThrow && currentDart == null)
        {
            // Awaiting Grip
            UpdateStatusUI("Dart Ready: Awaiting Grip", Color.black);
        }
    }

    // Instantiate dart and disable physics (held state)
    void PrepareDart()
    {
        if (dartPrefab == null) return;
        if (currentDart != null) Destroy(currentDart);

        // Instantiate at spawn point world position and rotation
        currentDart = Instantiate(dartPrefab, spawnPoint.position, spawnPoint.rotation);
        currentDart.transform.SetParent(spawnPoint); 
        currentRb = currentDart.GetComponent<Rigidbody>();

        // Ensure dart local rotation is aligned with spawnPoint's Z-axis for clean aiming
        currentDart.transform.localRotation = Quaternion.identity; 

        if (currentRb == null) 
        {
            Debug.LogError("Dart Prefab must have a Rigidbody component!");
            return;
        }

        currentRb.isKinematic = true;
        currentRb.useGravity = false;
    }
    
    // Updates dart rotation based on MPU Pitch/Roll
    void UpdateAiming(Transform dartTransform)
    {
        if (arduinoPackage == null) return; 

        float pitch = arduinoPackage.CurrentPitch;
        float roll = arduinoPackage.CurrentRoll;

        if (invertPitch) pitch *= -1;
        if (invertRoll) roll *= -1;

        // 🚨 MPU Frame Compensation: -90f on X-axis to align MPU's "Up" with game's "Forward"
        Quaternion targetRotation = Quaternion.Euler(pitch + rotationOffset.x - 90f, rotationOffset.y, -roll + rotationOffset.z);
        
        m_AimingRotation = targetRotation; 

        dartTransform.localRotation = Quaternion.Slerp(dartTransform.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    // ==========================================
    // 5. Throw and Cooldown (Final Logic)
    // ==========================================

    void ThrowDart(Vector3 sensorAccel)
    {
        lastThrowTime = Time.time;
        isReadyToThrow = false; 
        isGripping = false;     

        // Start Cooldown Coroutine (TimeScale independent)
        StartCoroutine(ReloadCooldownCoroutine(cooldownTime));
        
        currentDart.transform.SetParent(null);

        currentRb.isKinematic = false;
        currentRb.useGravity = true;
        
        float magnitude = sensorAccel.magnitude;
        float power = magnitude * forceMultiplier;
        
        // 1. Base Launch Direction (spawnPoint.forward)
        Vector3 baseLaunchDirection = spawnPoint.forward;
        
        // 2. Apply World Offset (e.g., 90/180 degree rotation compensation)
        Quaternion worldOffsetRotation = Quaternion.Euler(WorldLaunchOffset);
        baseLaunchDirection = worldOffsetRotation * baseLaunchDirection;
        
        // 3. Apply MPU Aiming Rotation to the base World Vector
        Vector3 finalThrowDirection = m_AimingRotation * baseLaunchDirection; 
        
        // 4. Final Direction Inversion Check
        if (invertThrowDirection)
        {
            finalThrowDirection = -finalThrowDirection;
        }
        
        currentRb.AddForce(finalThrowDirection * power, ForceMode.Impulse);
        
        Debug.Log($"<color=cyan>Dart Launched! Power: {power}</color>");

        if (followCamera != null)
        {
            followCamera.StartFollowing(currentDart.transform);
        }

        currentDart = null;
        currentRb = null;
    }

    // Cooldown Coroutine (TimeScale independent)
    IEnumerator ReloadCooldownCoroutine(float duration)
    {
        UpdateStatusUI($"Reloading... ({duration:F1}s)", Color.red);
        
        yield return new WaitForSecondsRealtime(duration);
        
        ReloadComplete(); 
    }

    // Called when cooldown finishes
    void ReloadComplete()
    {
        isReadyToThrow = true;
        
        // Return to Ready state. Gripping check is now done in Update().
        UpdateStatusUI("Dart Ready: Awaiting Grip", Color.black);
    }
}