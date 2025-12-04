using UnityEngine;
using TMPro;
using System.Collections;

public class DartThrower : MonoBehaviour
{
[Header("연결 요소")]
public ArduinoPackage arduinoPackage; 
public GameObject dartPrefab;      // 날아갈 다트 프리팹
public Transform spawnPoint;       // 다트가 생성될 위치 (손의 위치)
private FollowCamera followCamera; // FollowCamera 참조

[Header("UI 요소")]
// 🎯 추가: UI 텍스트 컴포넌트 연결 변수
public TextMeshProUGUI statusText; 

[Header("디버깅 설정")] 
public KeyCode debugThrowKey = KeyCode.Space;
public KeyCode debugGripKey = KeyCode.T;
public Vector3 debugAccel = new Vector3(3.0f, 0f, 0f);

// 내부 다트 관리 변수
private GameObject currentDart;
private Rigidbody currentRb;

[Header("던지기 설정")]
public float throwThreshold = 2.0f; 
public float forceMultiplier = 50.0f;
public float cooldownTime = 1.0f;

[Header("조준(기울기) 설정")]
public float rotationSmoothness = 10f; 
public Vector3 rotationOffset = new Vector3(0, 0, 0); 
public bool invertPitch = false; 
public bool invertRoll = false;

// 상태 관리 변수
private float lastThrowTime;
private bool isReadyToThrow = true; 
private bool isGripping = false;    

// DartThrower.cs 파일의 핵심 함수 수정

void Start()
{   
    arduinoPackage = FindObjectOfType<ArduinoPackage>(); // 🚨 누락되었던 FindObjectOfType을 Start에 추가
    followCamera = FindObjectOfType<FollowCamera>();
    
    // 🚨 연결 안정성 확보: 아두이노가 있다면 연결 시도
    if (arduinoPackage != null && !arduinoPackage.IsConnected)
    {
        arduinoPackage.Connect();
    }
    
    UpdateStatusUI("다트 준비 완료: 그립 대기");
}

void Update()
{
    // 🚨 1. 디버깅/Null 체크: arduinoPackage가 null이면 키보드 디버깅 모드
    bool isDebugging = (arduinoPackage == null);
    
    if (!isDebugging)
    {
        // 🚨 아두이노가 연결되어 있으면 시리얼 통신 읽기
        arduinoPackage.ReadSerialLoop();
    }

    // 2. 그립(준비) 상태 업데이트
    UpdateGrippingState(isDebugging); // 🚨 isDebugging 플래그 전달

    // 3. 그립 중인 경우에만 조준 및 던지기 감지
    if (isGripping && isReadyToThrow)
    {
        Vector3 currentAccel;
        bool shouldThrow;

        // 🎯 조준: 현재 다트 인스턴스가 있을 때만 회전
        if (currentDart != null)
        {
            // 🚨 디버깅 모드에서는 조준 로직을 건너뜁니다.
            if (!isDebugging)
            {
                UpdateAiming(currentDart.transform);
            }
        }
        
        // 🚨 4. 던지기 감지 로직 통합
        if (isDebugging)
        {
            currentAccel = debugAccel;
            shouldThrow = Input.GetKeyDown(debugThrowKey);
        }
        else
        {
            currentAccel = new Vector3(
                arduinoPackage.RawAccelX, 
                arduinoPackage.RawAccelY, 
                arduinoPackage.RawAccelZ
            );
            // 가속도 임계값 체크
            shouldThrow = currentAccel.magnitude > throwThreshold;
        }

        if (currentDart != null && shouldThrow && Time.time > lastThrowTime + cooldownTime)
        {
            ThrowDart(currentAccel);
        }
    }
}

// 🚨 [수정됨] Invoke 대신 코루틴 사용 및 UI/타이머 오류 수정
void ThrowDart(Vector3 sensorAccel)
{
    lastThrowTime = Time.time;
    isReadyToThrow = false; 
    isGripping = false;     

    // 🚨 Invoke 대신 코루틴 시작 (TimeScale 무시)
    StartCoroutine(ReloadCooldownCoroutine(cooldownTime));
    
    currentDart.transform.SetParent(null);

    currentRb.isKinematic = false;
    currentRb.useGravity = true;
    
    float power = sensorAccel.magnitude * forceMultiplier;
    currentRb.AddForce(currentDart.transform.forward * power, ForceMode.Impulse);
    
    Debug.Log($"<color=cyan>다트 발사! Power: {power}</color>");

    if (followCamera != null)
    {
        followCamera.StartFollowing(currentDart.transform);
    }

    currentDart = null;
    currentRb = null;
}

// 🚨 [새 함수] TimeScale에 영향을 받지 않는 재장전 코루틴
IEnumerator ReloadCooldownCoroutine(float duration)
{
    UpdateStatusUI($"재장전 중... ({duration:F1}초)", Color.red);
    // WaitForSecondsRealtime을 사용해 TimeScale=0 이어도 시간이 흐름
    yield return new WaitForSecondsRealtime(duration);
    
    ReloadComplete(); 
}

// 🚨 [수정됨] UpdateGrippingState 시그니처 변경 및 Null 체크 추가
void UpdateGrippingState(bool isDebugging)
{
    bool touchPressed;
    
    // 🚨 입력 대체 로직
    if (isDebugging)
    {
        touchPressed = Input.GetKey(debugGripKey); 
    }
    else
    {
        if (arduinoPackage == null) return; // 🚨 Null 안전성
        touchPressed = arduinoPackage.IsTouchPressed;
    }

    if (touchPressed && !isGripping && isReadyToThrow)
    {
        isGripping = true;
        PrepareDart();
        UpdateStatusUI("조준 중... (발사 대기)", Color.green);
    }
    else if (!touchPressed && isGripping)
    {
        // ... (그립 해제 로직 유지) ...
        isGripping = false;
        if (currentDart != null)
        {
            Destroy(currentDart);
            currentDart = null;
            currentRb = null;
            UpdateStatusUI("다트 취소됨: 그립 대기", Color.black);
        }
    }
    // 🚨 쿨타임이 끝났을 때만 UI를 Ready로 복구 (Reloading 오류 해결)
    else if (!isGripping && isReadyToThrow && currentDart == null)
    {
        UpdateStatusUI("다트 준비 완료: 그립 대기", Color.black);
    }
}

    // 🎯 UI 업데이트 헬퍼 함수
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

    // Touch 버튼 상태 변화에 따라 다트를 잡거나 놓음
    void UpdateGrippingState()
    {
        bool touchPressed = arduinoPackage.IsTouchPressed;

        if (touchPressed && !isGripping && isReadyToThrow)
        {
            // 버튼이 눌림 -> 그립 시작 및 다트 인스턴스 생성
            isGripping = true;
            PrepareDart();
            UpdateStatusUI("🎯 조준 중... (발사 대기)", Color.green);
        }
        else if (!touchPressed && isGripping)
        {
            // 버튼이 떼어짐 -> 그립 해제 및 다트 제거 (던지지 않은 경우)
            isGripping = false;
            if (currentDart != null)
            {
                Destroy(currentDart);
                currentDart = null;
                currentRb = null;
                UpdateStatusUI("다트 취소됨: 그립 대기", Color.black);
            }
        }
        // 버튼이 눌리지 않았고, 쿨타임이 끝났을 때
        else if (!isGripping && isReadyToThrow && currentDart == null)
        {
            UpdateStatusUI("다트 준비 완료: 그립 대기", Color.black);
        }
    }

    // 다트를 생성하고 물리 설정 비활성화 (손에 들고 있는 상태)
    void PrepareDart()
    {
        // ... (이전 코드와 동일) ...
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
        // ------------------------
    }
    
    // 기울기를 받아 다트의 회전을 업데이트 (이전 코드와 동일)
    void UpdateAiming(Transform dartTransform)
    {
        float pitch = arduinoPackage.CurrentPitch;
        float roll = arduinoPackage.CurrentRoll;

        if (invertPitch) pitch *= -1;
        if (invertRoll) roll *= -1;

        Quaternion targetRotation = Quaternion.Euler(pitch + rotationOffset.x, rotationOffset.y, -roll + rotationOffset.z);
        dartTransform.localRotation = Quaternion.Slerp(dartTransform.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    // 재장전이 완료되면 다시 발사 가능 상태로 복귀
    void ReloadComplete()
    {
        isReadyToThrow = true;
        
        // 쿨타임이 끝났을 때 상태에 따른 UI 업데이트
        if (arduinoPackage.IsTouchPressed) 
        {
            // Touch 버튼이 눌려 있다면 바로 그립 상태로 전환
            isGripping = true;
            PrepareDart();
            UpdateStatusUI("🎯 조준 중... (발사 대기)", Color.green);
        }
        else 
        {
            // Touch 버튼이 눌려 있지 않다면 그립 대기 상태로 전환
            isGripping = false;
            UpdateStatusUI("다트 준비 완료: 그립 대기", Color.white);
        }
    }
}