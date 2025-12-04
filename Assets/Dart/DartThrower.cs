using UnityEngine;
using TMPro; 
using System.Collections; 

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
    private Quaternion m_AimingRotation = Quaternion.identity; 

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
        
        if (arduinoPackage != null && !arduinoPackage.IsConnected)
        {
            arduinoPackage.Connect();
        }
        
        followCamera = FindObjectOfType<FollowCamera>();
    
        UpdateStatusUI("Dart is Ready. Aim by moving MPU.");
    }

    void Update()
    {
        bool isDebugging = (arduinoPackage == null || !arduinoPackage.IsConnected);
        
        if (!isDebugging)
        {
            if (arduinoPackage == null) return;
            arduinoPackage.ReadSerialLoop();
        }
        
        // 🚨 1. 터치 센서 상태 확인 (발사 결정용)
        bool touchPressed = isDebugging ? Input.GetKey(debugGripKey) : (arduinoPackage != null && arduinoPackage.IsTouchPressed);

        // 🚨 2. 다트 준비 (Aiming 상태 유지): 다트가 없고 쿨타임이 끝났으면 무조건 생성
        if (currentDart == null && isReadyToThrow)
        {
            PrepareDart();
            UpdateStatusUI("Aiming Ready. Touch Sensor is OFF.");
        }
        
        // 🚨 3. 조준 상태 업데이트 (다트가 존재하면 항상 실행)
        if (currentDart != null)
        {
            UpdateAiming(currentDart.transform);
        }

        // 🚨 4. 발사 조건 체크 (터치가 눌리고, 쿨타임이 끝났을 때만 가속도 체크)
        if (currentDart != null && isReadyToThrow && touchPressed)
        {
            float actualAccel = 0f;
            bool shouldThrow = false;
            
            if (isDebugging)
            {
                shouldThrow = Input.GetKeyDown(debugThrowKey);
                actualAccel = debugAccel; 
            }
            else
            {
                // 아두이노 모드: RawAccelX의 절대값이 임계값 초과 시 던지기
                shouldThrow = Mathf.Abs(arduinoPackage.RawAccelX) > throwThreshold;
                actualAccel = arduinoPackage.RawAccelX;
            }

            if (shouldThrow)
            {
                ThrowDart(actualAccel); 
            }
            else
            {
                // 터치 센서가 눌렸지만 가속도가 부족할 때
                UpdateStatusUI("Touching... Release to cancel. Throw harder!");
            }
        }
        else if (currentDart != null && !isReadyToThrow)
        {
            // 쿨다운 중일 때
            UpdateStatusUI($"Reloading... ({(lastThrowTime + cooldownTime) - Time.time:F1}s)", Color.red);
        }
        else if (currentDart != null && !touchPressed)
        {
             // 조준 중일 때
             UpdateStatusUI("Aiming Ready. Touch Sensor is OFF.");
        }
    }
    
    // 🚨 UpdateGrippingState 함수는 더 이상 사용하지 않습니다.

    // ==========================================
    // 4. 다트 준비 (회전 보정 제거)
    // ==========================================

    void UpdateStatusUI(string message, Color color = default)
    {
        if (statusText != null)
        {
            statusText.text = message;
            if (color != default) 
            {
                statusText.color = color; 
            }
            else 
            {
                statusText.color = Color.black;
            }
        }
    }

    void PrepareDart()
    {
        if (dartPrefab == null) return;
        if (currentDart != null) Destroy(currentDart);

        currentDart = Instantiate(dartPrefab, spawnPoint.position, spawnPoint.rotation);
        currentDart.transform.SetParent(spawnPoint); 
        
        // 로컬 회전 강제 정렬 (프리팹의 로컬 Z축이 spawnPoint의 Z축을 향하도록 보장)
        currentDart.transform.localRotation = Quaternion.identity; 

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
    // 5. 발사 및 쿨다운 (최종 보정 로직 유지)
    // ==========================================
    
    void UpdateAiming(Transform dartTransform)
    {
        if (arduinoPackage == null) return; 

        float pitch = arduinoPackage.CurrentPitch;
        float roll = arduinoPackage.CurrentRoll;

        if (invertPitch) pitch *= -1;
        if (invertRoll) roll *= -1;

        // 🚨 MPU 프레임 정렬 보정 (-90도)
        Quaternion targetRotation = Quaternion.Euler(pitch + rotationOffset.x - 90f, rotationOffset.y, -roll + rotationOffset.z);
        
        m_AimingRotation = targetRotation; 

        dartTransform.localRotation = Quaternion.Slerp(dartTransform.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    void ThrowDart(float sensorAccel)
    {
        lastThrowTime = Time.time;
        isReadyToThrow = false; 

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
        
        // 2. MPU 회전을 베이스 벡터에 적용하여 최종 발사 벡터 계산
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
        
        // ForSecondsRealtime을 사용하여 Time.timeScale에 영향을 받지 않음
        yield return new WaitForSecondsRealtime(duration); 
        
        ReloadComplete(); 
    }

    // 재장전이 완료되면 다시 발사 가능 상태로 복귀
    void ReloadComplete()
    {
        isReadyToThrow = true;
        UpdateStatusUI("Dart is Ready. Aim by moving MPU.");
    }

    void OnApplicationQuit()
    {
        if (arduinoPackage != null) arduinoPackage.Disconnect();
    }
}