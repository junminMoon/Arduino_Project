using UnityEngine;
using TMPro; 
using System.Collections; // 코루틴 사용

public class DartThrower : MonoBehaviour
{
    // ==========================================
    // 1. 연결 및 UI 요소
    // ==========================================
    [Header("연결 요소")]
    private ArduinoPackage arduinoPackage; 
    public GameObject dartPrefab;      
    public Transform spawnPoint;       
    private FollowCamera followCamera;
    public TextMeshProUGUI statusText; 

    // 내부 다트 관리 변수
    private GameObject currentDart;
    private Rigidbody currentRb;

    // ==========================================
    // 2. 던지기 및 조준 설정
    // ==========================================
    [Header("던지기 설정")]
    public float throwThreshold = 2.0f;
    public float forceMultiplier = 50.0f; 
    public float cooldownTime = 1.0f;

    [Header("조준(기울기) 설정")]
    public float rotationSmoothness = 10f; 
    public Vector3 rotationOffset = new Vector3(0, 0, 0); 
    public bool invertPitch = false; 
    public bool invertRoll = false;

    [Header("발사 방향 보정")]
    [Tooltip("다트가 뒤로 날아간다면 이것을 체크하여 힘의 방향을 반전시킵니다.")]
    public bool invertThrowDirection = false; 
    [Tooltip("MPU와 월드 좌표계 오차 보정용. 수평/수직 정렬을 위해 90 또는 -90으로 조정.")]
    public Vector3 WorldLaunchOffset = new Vector3(0, 0, 0); 

    [Header("디버깅 설정")] 
    public KeyCode debugThrowKey = KeyCode.Space;
    public KeyCode debugGripKey = KeyCode.T;
    public float debugAccel = 3.0f; 

    // ==========================================
    // 3. 상태 관리 변수
    // ==========================================
    private float lastThrowTime;
    private bool isReadyToThrow = true; 
    private bool isGripping = false;    
    private Quaternion m_AimingRotation = Quaternion.identity; // MPU 조준 회전값 저장용

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
        
        if (arduinoPackage != null && !arduinoPackage.IsConnected)
        {
            arduinoPackage.Connect();
        }
        
        followCamera = FindObjectOfType<FollowCamera>();
    
        UpdateStatusUI("Dart is Ready, please Touch!");
    }

    void Update()
    {
        bool isDebugging = (arduinoPackage == null || !arduinoPackage.IsConnected);
        
        if (!isDebugging)
        {
            if (arduinoPackage == null) return;
            arduinoPackage.ReadSerialLoop();
        }
        
        UpdateGrippingState(isDebugging); 

        if (isGripping && isReadyToThrow)
        {
            if (currentDart != null && !isDebugging)
            {
                UpdateAiming(currentDart.transform);
            }

            bool shouldThrow = false;
            float actualAccel = 0f;

            Vector3 currentAccel = new Vector3(
                arduinoPackage.RawAccelX, 
                arduinoPackage.RawAccelY, 
                arduinoPackage.RawAccelZ
            );

            if (isDebugging)
            {
                shouldThrow = Input.GetKeyDown(debugThrowKey) && Time.time > lastThrowTime + cooldownTime;
                actualAccel = debugAccel; 
            }
            else
            {
                shouldThrow = Mathf.Abs(currentAccel.magnitude) > throwThreshold && Time.time > lastThrowTime + cooldownTime;
                actualAccel = currentAccel.magnitude;
            }

            if (currentDart != null && shouldThrow)
            {
                ThrowDart(actualAccel); 
            }
        }
    }

    // ==========================================
    // 4. 상태 관리 및 UI
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

    void UpdateGrippingState(bool isDebugging)
    {
        bool touchPressed = false;
        
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
            isGripping = true;
            PrepareDart();
            
            string debugKeyMsg = isDebugging ? $" (Key: {debugThrowKey.ToString()})" : "";
            UpdateStatusUI("Aiming" + debugKeyMsg, Color.green); 
        }
        else if (!touchPressed && isGripping)
        {
            isGripping = false;
            if (currentDart != null)
            {
                Destroy(currentDart);
                currentDart = null;
                currentRb = null;
                UpdateStatusUI("Dart Cancel, please Touch!", Color.black);
            }
        }
        else if (!isGripping && isReadyToThrow && currentDart == null)
        {
            UpdateStatusUI("Dart is Ready, please Touch!", Color.black);
        }
    }

    void PrepareDart()
    {
        if (dartPrefab == null) return;
        if (currentDart != null) Destroy(currentDart);

        currentDart = Instantiate(dartPrefab, spawnPoint.position, spawnPoint.rotation);
        currentDart.transform.SetParent(spawnPoint); 
        
        // 🚨 프리팹의 로컬 회전은 그대로 유지합니다. (수동 보정 제거)

        currentRb = currentDart.GetComponent<Rigidbody>();

        if (currentRb == null) 
        {
            Debug.LogError("Dart Prefab에 Rigidbody가 없습니다. 추가해주세요!");
            return;
        }

        currentRb.isKinematic = true;
        currentRb.useGravity = false;
    }
    
    // ==========================================
    // 5. 발사 및 쿨다운 (최종 보정 로직 포함)
    // ==========================================
    
    void UpdateAiming(Transform dartTransform)
    {
        if (arduinoPackage == null) return; 

        float pitch = arduinoPackage.CurrentPitch;
        float roll = arduinoPackage.CurrentRoll;

        if (invertPitch) pitch *= -1;
        if (invertRoll) roll *= -1;

        // 🚨 최종 수정: MPU 프레임 정렬을 위해 Pitch에 -90도 보정을 적용합니다.
        Quaternion targetRotation = Quaternion.Euler(pitch + rotationOffset.x - 90f, rotationOffset.y, -roll + rotationOffset.z);
        
        m_AimingRotation = targetRotation; 

        dartTransform.localRotation = Quaternion.Slerp(dartTransform.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    void ThrowDart(float sensorAccel)
    {
        lastThrowTime = Time.time;
        isReadyToThrow = false; 
        isGripping = false;     

        StartCoroutine(ReloadCooldownCoroutine(cooldownTime));
        
        currentDart.transform.SetParent(null);

        currentRb.isKinematic = false;
        currentRb.useGravity = true;
        
        float magnitude = Mathf.Abs(sensorAccel);
        float power = magnitude * forceMultiplier;
        
        // 1. 월드 베이스 발사 방향 (spawnPoint.forward)에 월드 오프셋 적용
        Vector3 baseLaunchDirection = spawnPoint.forward;
        Quaternion worldOffsetRotation = Quaternion.Euler(WorldLaunchOffset);
        baseLaunchDirection = worldOffsetRotation * baseLaunchDirection;
        
        // 2. MPU 회전을 베이스 벡터에 적용하여 최종 발사 벡터 계산 (핵심 수정)
        Vector3 finalThrowDirection = m_AimingRotation * baseLaunchDirection; 
        
        if (invertThrowDirection)
        {
            finalThrowDirection = -finalThrowDirection;
        }
        
        currentRb.AddForce(finalThrowDirection * power, ForceMode.Impulse);
        
        Debug.Log($"<color=cyan>다트 발사! Power: {power}</color>");

        if (followCamera != null)
        {
            followCamera.StartFollowing(currentDart.transform);
        }

        currentDart = null;
        currentRb = null;
    }

    // 쿨타임 코루틴 (TimeScale 무시)
    IEnumerator ReloadCooldownCoroutine(float duration)
    {
        UpdateStatusUI($"Reloading... ({duration:F1}s)", Color.red);
        
        yield return new WaitForSecondsRealtime(duration);
        
        ReloadComplete(); 
    }

    // 재장전이 완료되면 다시 발사 가능 상태로 복귀
    void ReloadComplete()
    {
        if (arduinoPackage == null)
        {
            isReadyToThrow = true;
            isGripping = false;
            UpdateStatusUI("Dart is Ready, please Touch!", Color.black);
            return;
        }

        isReadyToThrow = true;
        
        if (arduinoPackage.IsTouchPressed) 
        {
            isGripping = true;
            PrepareDart();
            UpdateStatusUI("Aiming...", Color.green);
        }
        else 
        {
            isGripping = false;
            UpdateStatusUI("Dart is Ready, please Touch!", Color.black);
        }
    }

    void OnApplicationQuit()
    {
        if (arduinoPackage != null) arduinoPackage.Disconnect();
    }
}