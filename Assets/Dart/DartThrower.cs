using UnityEngine;
using TMPro; // TextMeshPro 사용
using System.Collections; // 코루틴(IEnumerator) 사용

public class DartThrower : MonoBehaviour
{
    // ==========================================
    // 1. 연결 및 UI 요소
    // ==========================================
    [Header("연결 요소")]
    private ArduinoPackage arduinoPackage; 
    public GameObject dartPrefab;      // 날아갈 다트 프리팹
    public Transform spawnPoint;       // 다트가 생성될 위치
    private FollowCamera followCamera; // FollowCamera 참조 (씬에 있어야 함)
    public TextMeshProUGUI statusText; 

    // 내부 다트 관리 변수
    private GameObject currentDart;
    private Rigidbody currentRb;

    // ==========================================
    // 2. 던지기 및 조준 설정
    // ==========================================
    [Header("던지기 설정")]
    public float throwThreshold = 2.0f; 
    public float forceMultiplier = 50.0f; // 💡 포물선 궤적을 위해 이 값을 5.0f ~ 15.0f로 낮춰 테스트해보세요.
    public float cooldownTime = 1.0f;

    [Header("조준(기울기) 설정")]
    public float rotationSmoothness = 10f; 
    public Vector3 rotationOffset = new Vector3(0, 0, 0); 
    public bool invertPitch = false; 
    public bool invertRoll = false;

    [Header("디버깅 설정")] 
    public KeyCode debugThrowKey = KeyCode.Space;
    public KeyCode debugGripKey = KeyCode.T;
    public float debugAccel = 3.0f; 

    // ==========================================
    // 3. 상태 관리 변수
    // ==========================================
    private float lastThrowTime;
    private bool isReadyToThrow = true; // 쿨타임이 끝났는지 (던질 준비가 되었는지)
    private bool isGripping = false;    // 현재 그립(터치) 중인지

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
        
        // 🚨 Null 안전성: 아두이노 패키지가 있고 연결되어 있지 않다면 연결 시도
        if (arduinoPackage != null && !arduinoPackage.IsConnected)
        {
            arduinoPackage.Connect();
        }
        
        followCamera = FindObjectOfType<FollowCamera>();
    
        UpdateStatusUI("Dart is Ready, please Touch!");
    }

    void Update()
    {
        // 🚨 디버깅 모드 확인 (ArduinoPackage가 없거나 연결이 끊어졌으면 true)
        bool isDebugging = (arduinoPackage == null || !arduinoPackage.IsConnected);
        
        // 1. 시리얼 통신 읽기 (디버깅 모드에서는 건너뜀)
        if (!isDebugging)
        {
            arduinoPackage.ReadSerialLoop();
        }
        
        // 2. 그립 상태 업데이트 (디버깅 플래그 전달)
        UpdateGrippingState(isDebugging); 

        // 3. 발사 감지 및 조준 로직
        if (isGripping && isReadyToThrow)
        {
            // 조준: 현재 다트가 있고 디버깅 모드가 아닐 때만 회전
            if (currentDart != null && !isDebugging)
            {
                UpdateAiming(currentDart.transform);
            }

            // 발사 조건 체크
            bool shouldThrow = false;
            float actualAccel = 0f;

            if (isDebugging)
            {
                shouldThrow = Input.GetKeyDown(debugThrowKey) && Time.time > lastThrowTime + cooldownTime;
                actualAccel = debugAccel; 
            }
            else
            {
                shouldThrow = arduinoPackage.RawAccelY > throwThreshold && Time.time > lastThrowTime + cooldownTime;
                actualAccel = arduinoPackage.RawAccelY;
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
            // 그립 시작 (쿨타임 X)
            isGripping = true;
            PrepareDart();
            // 🚨 UI 수정: 그립 시작 시 "조준 중" 메시지 표시 (Reloading 아님)
            string debugKeyMsg = isDebugging ? $" (Key: {debugThrowKey})" : "";
            UpdateStatusUI("Aiming" + debugKeyMsg, Color.green); 
        }
        else if (!touchPressed && isGripping)
        {
            // 그립 해제 및 다트 제거 (던지지 않은 경우)
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
            // 그립 대기
            UpdateStatusUI("Dart is Ready, please Touch!", Color.black);
        }
    }

    void PrepareDart()
    {
        if (dartPrefab == null) return;
        if (currentDart != null) Destroy(currentDart);

        currentDart = Instantiate(dartPrefab, spawnPoint.position, spawnPoint.rotation);
        currentDart.transform.SetParent(spawnPoint); 
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
    // 5. 발사 및 쿨다운
    // ==========================================
    
    void UpdateAiming(Transform dartTransform)
    {
        if (arduinoPackage == null) return; 

        float pitch = arduinoPackage.CurrentPitch;
        float roll = arduinoPackage.CurrentRoll;

        if (invertPitch) pitch *= -1;
        if (invertRoll) roll *= -1;

        Quaternion targetRotation = Quaternion.Euler(pitch + rotationOffset.x, rotationOffset.y, -roll + rotationOffset.z);
        dartTransform.localRotation = Quaternion.Slerp(dartTransform.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    void ThrowDart(float sensorAccel)
    {
        lastThrowTime = Time.time;
        isReadyToThrow = false; 
        isGripping = false;     

        // 🚨 쿨타임 코루틴 시작
        StartCoroutine(ReloadCooldownCoroutine(cooldownTime));
        
        // 🎯 발사 로직
        currentDart.transform.SetParent(null);

        currentRb.isKinematic = false;
        currentRb.useGravity = true;
        
        float power = sensorAccel * forceMultiplier;
        currentRb.AddForce(currentDart.transform.forward * power, ForceMode.Impulse);
        
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
        
        // 🚨 TimeScale에 관계없이 실제 시간만큼 기다립니다.
        yield return new WaitForSecondsRealtime(duration);
        
        ReloadComplete(); 
    }

    // 재장전이 완료되면 다시 발사 가능 상태로 복귀
    void ReloadComplete()
    {
        // 🚨 Null 안전성
        if (arduinoPackage == null)
        {
            isReadyToThrow = true;
            isGripping = false;
            UpdateStatusUI("Dart is Ready, please Touch!", Color.black);
            return;
        }

        isReadyToThrow = true;
        
        // 쿨타임이 끝난 후 Touch 상태 확인
        if (arduinoPackage.IsTouchPressed) 
        {
            // Touch 버튼이 눌려 있다면 바로 그립 상태로 전환
            isGripping = true;
            PrepareDart();
            UpdateStatusUI("Aiming...", Color.green);
        }
        else 
        {
            // Touch 버튼이 눌려 있지 않다면 그립 대기 상태로 전환
            isGripping = false;
            UpdateStatusUI("Dart is Ready, please Touch!", Color.black);
        }
    }

    void OnApplicationQuit()
    {
        if (arduinoPackage != null) arduinoPackage.Disconnect();
    }
}