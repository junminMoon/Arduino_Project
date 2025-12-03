using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 네임스페이스 추가

public class DartThrower : MonoBehaviour
{
    [Header("연결 요소")]
    private ArduinoPackage arduinoPackage; 
    public GameObject dartPrefab;      // 날아갈 다트 프리팹
    public Transform spawnPoint;       // 다트가 생성될 위치 (손의 위치)
    private FollowCamera followCamera; // FollowCamera 참조
    
    [Header("UI 요소")]
    public TextMeshProUGUI statusText; 

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
        if (arduinoPackage == null) return;

        arduinoPackage.ReadSerialLoop();
        
        UpdateGrippingState();

        // 3. 그립 중인 경우에만 조준 및 던지기 감지
        if (isGripping && isReadyToThrow)
        {
            // 조준: 현재 다트 인스턴스가 있을 때만 회전
            if (currentDart != null)
            {
                UpdateAiming(currentDart.transform);
            }

            
            if (currentDart != null && arduinoPackage.RawAccelX > throwThreshold && Time.time > lastThrowTime + cooldownTime)
            {
                ThrowDart(arduinoPackage.RawAccelX);
            }
        }
    }

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
        Debug.Log(arduinoPackage.IsTouchPressed);

        if (touchPressed && !isGripping && isReadyToThrow)
        {
            // 버튼이 눌림 -> 그립 시작 및 다트 인스턴스 생성
            isGripping = true;
            PrepareDart();
            UpdateStatusUI("Reloading... ", Color.green);
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
                UpdateStatusUI("Dart Cancel, please Touch!", Color.black);
            }
        }
        // 버튼이 눌리지 않았고, 쿨타임이 끝났을 때
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
    
    void UpdateAiming(Transform dartTransform)
    {
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

        // 🎯 발사 시 UI 업데이트
        UpdateStatusUI($"Reloding... ({cooldownTime:F1}sec)", Color.red);
        
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

        // 재장전이 완료될 때까지 남은 시간 계산 및 예약
        Invoke("ReloadComplete", cooldownTime);
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
            UpdateStatusUI("Reloading....", Color.green);
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