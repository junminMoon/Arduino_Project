using UnityEngine;
using System.IO.Ports;
using System.Globalization;
using System;
using System.Collections;

public class ArduinoPackageKart : MonoBehaviour
{
    // ==========================================
    // 1. 인스펙터 설정 (Inspector Settings)
    // ==========================================
    [Header("모드 선택 (Mode Selection)")]
    public bool useUsbMode = false; 

    [Header("블루투스 설정 (Wireless)")]
    [SerializeField] private string btPortName = "COM8"; // 블루투스 포트
    [SerializeField] private int btBaudRate = 9600;      // 블루투스 속도

    [Header("유선 USB 설정 (Wired)")]
    [SerializeField] private string usbPortName = "COM5"; // USB 포트 
    [SerializeField] private int usbBaudRate = 115200;    // USB 속도 

    [Header("필터 및 보정 설정")]
    [SerializeField] private float filterWeight = 0.90f; // (0.9 추천)
    [SerializeField] private float deadZone = 0.15f;


    // ==========================================
    // 2. 외부 공개 데이터 (Properties)
    // ==========================================
    // 연결 상태 변수
    public bool IsConnected { get; private set; }

    // 현재 연결된 모드 정보
    public string CurrentPortName { get; private set; }
    public int CurrentBaudRate { get; private set; }

    // [MPU6050] 기울기 값
    public float CurrentPitch { get; private set; }
    public float CurrentRoll { get; private set; }

    // RAW Data
    public float RawGyroX { get; private set; }
    public float RawGyroY { get; private set; }
    public float RawGyroZ { get; private set; }
    public float RawAccelX { get; private set; }
    public float RawAccelY { get; private set; }
    public float RawAccelZ { get; private set; }

    // [Joystick] 조이스틱 값
    public float JoyX { get; private set; }
    public float JoyY { get; private set; }
    public bool IsJoyPressed { get; private set; }

    // [Buttons] 버튼 값
    public bool IsButtonAPressed { get; private set; }
    public bool IsButtonBPressed { get; private set; }
    public bool IsButtonXPressed { get; private set; }
    public bool IsButtonYPressed { get; private set; }
    public bool IsTouchPressed { get; private set; }

    private bool m_PrevIsButtonAPressed;
    private bool m_PrevIsButtonBPressed;
    private bool m_PrevIsButtonXPressed;
    private bool m_PrevIsButtonYPressed;
    private bool m_PrevIsJoyPressed;

    // 일회성 버튼 입력 값
    public bool IsButtonADown { get; private set; }
    public bool IsButtonBDown { get; private set; }
    public bool IsButtonXDown { get; private set; }
    public bool IsButtonYDown { get; private set; }
    public bool IsJoyDown { get; private set; }

    // 내부 변수
    private SerialPort serialPort;

    private float _calcPitch, _calcRoll;

    private float lastPingTime = 0f;
    private const float PingInterval = 1.0f;
    private const float ArduinoDt = 0.1f; // 전송 주기

    private const int MaxConnectionAttempts = 10;
    private const float RetryDelay = 0.5f; // 재시도 간격 (0.5초)
    private Coroutine connectCoroutine; // 코루틴 참조 변수


    void Start()
    {
        connectCoroutine = StartCoroutine(AttemptConnectionCoroutine());
    }

    void Update()
    {
        ReadSerialLoop();
    }
    
    void OnApplicationQuit()
    {
        Disconnect();
    }


    void LateUpdate()
    {
        IsButtonADown = false;
        IsButtonBDown = false;
        IsButtonXDown = false;
        IsButtonYDown = false;
        IsJoyDown = false;
    }
    
    // ==========================================
    // 3. 연결 및 해제
    // ==========================================
    public void Connect()
    {
        if (IsConnected) return;

        // ★ 모드에 따라 포트와 속도 자동 선택
        if (useUsbMode)
        {
            CurrentPortName = usbPortName;
            CurrentBaudRate = usbBaudRate;
        }
        else
        {
            CurrentPortName = btPortName;
            CurrentBaudRate = btBaudRate;
        }

        try
        {
            serialPort = new SerialPort(CurrentPortName, CurrentBaudRate);
            serialPort.ReadTimeout = 10;
            serialPort.Open();
            IsConnected = true;

            // 연결 성공 시 초기화
            _calcPitch = 0f; _calcRoll = 0f;

            Debug.Log($"<color=green>아두이노 연결 성공! [{CurrentPortName} @ {CurrentBaudRate}]</color>");
        }
        catch (System.Exception ex)
        {
            IsConnected = false;
            Debug.LogError($"<color=red>연결 실패 ({CurrentPortName}): {ex.Message}</color>");
        }
    }

    public void Disconnect()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            IsConnected = false;
        }
    }
    // ==========================================
    // 4. 메인 루프
    // ==========================================
    public void ReadSerialLoop()
    {
        if (!IsConnected || serialPort == null || !serialPort.IsOpen) return;

        if (Time.time - lastPingTime > PingInterval)
        {
            try
            {
                serialPort.WriteLine("P");
                lastPingTime = Time.time;
            }
            catch {}
        }

        // 데이터 수신
        try
        {
            string rawData = serialPort.ReadLine();
            DispatchData(rawData);
        }
        catch (System.TimeoutException) { }
        catch (System.Exception ex) { Debug.LogWarning($"데이터 오류: {ex.Message}"); }
    }

    // ==========================================
    // 5. 데이터 분류 및 처리
    // ==========================================
    private void DispatchData(string data)
    {
        if (string.IsNullOrEmpty(data)) return;
        string[] parts = data.Split(' ');
        if (parts.Length < 2) return;

        string key = parts[0];
        string value = parts[1];

        switch (key)
        {
            case "G": ProcessMPU(value); break;
            case "J": ProcessJoystick(value); break;
            case "X":
            case "Y":
            case "B":
            case "A":
            case "T":
                ProcessButtons(key, value); break;
        }
    }

    // 기울기 값 처리
    private void ProcessMPU(string csvData)
    {
        string[] values = csvData.Split(',');
        if (values.Length != 6) return;

        try
        {
            float gx = float.Parse(values[0], CultureInfo.InvariantCulture);
            float gy = float.Parse(values[1], CultureInfo.InvariantCulture);
            float gz = float.Parse(values[2], CultureInfo.InvariantCulture);
            float ax = float.Parse(values[3], CultureInfo.InvariantCulture);
            float ay = float.Parse(values[4], CultureInfo.InvariantCulture);
            float az = float.Parse(values[5], CultureInfo.InvariantCulture);

            RawGyroX = gx; RawGyroY = gy; RawGyroZ = gz;
            RawAccelX = ax; RawAccelY = ay; RawAccelZ = az;

            CalculateComplementaryFilter(gx, gy, ax, ay, az);
        }
        catch { }
    }

    // 조이스틱 값 처리
    private void ProcessJoystick(string csvData)
    {
        string[] values = csvData.Split(',');
        if (values.Length == 3)
        {
            try
            {
                float rawX = float.Parse(values[0], CultureInfo.InvariantCulture);
                float rawY = float.Parse(values[1], CultureInfo.InvariantCulture);
                JoyX = ApplyDeadzone(-MapValue(rawX));
                JoyY = ApplyDeadzone(MapValue(rawY));
                int sw = int.Parse(values[2], CultureInfo.InvariantCulture);
                IsJoyPressed = (sw == 0);
                IsJoyDown = (sw == 0) && !m_PrevIsButtonAPressed;
                m_PrevIsJoyPressed = (sw == 0);
            }
            catch { }
        }
    }

    // 버튼 값 처리
    private void ProcessButtons(string key, string state)
    {
        bool isPressed = (state == "1");

        // 이전 상태 업데이트를 위한 함수 호출
        UpdateDownStates(key, isPressed); 

        // 현재 상태 업데이트
        if (key == "X") IsButtonXPressed = isPressed;
        else if (key == "Y") IsButtonYPressed = isPressed;
        else if (key == "B") IsButtonBPressed = isPressed;
        else if (key == "A") IsButtonAPressed = isPressed;
        else if (key == "T") IsTouchPressed = isPressed;
    }


    private void UpdateDownStates(string key, bool isCurrentPressed)
    {
        // 'A' 버튼에 대한 눌림 순간 감지
        if (key == "A")
        {
            IsButtonADown = isCurrentPressed && !m_PrevIsButtonAPressed;
            
            m_PrevIsButtonAPressed = isCurrentPressed;
        }
        // 'B' 버튼에 대한 눌림 순간 감지
        if (key == "B")
        {
            IsButtonBDown = isCurrentPressed && !m_PrevIsButtonBPressed;
            
            m_PrevIsButtonBPressed = isCurrentPressed;
        }
        // 'X' 버튼에 대한 눌림 순간 감지
        if (key == "X")
        {
            IsButtonXDown = isCurrentPressed && !m_PrevIsButtonXPressed;
            
            m_PrevIsButtonXPressed = isCurrentPressed;
        }
        // 'Y' 버튼에 대한 눌림 순간 감지
        if (key == "Y")
        {
            IsButtonYDown = isCurrentPressed && !m_PrevIsButtonYPressed;
            
            m_PrevIsButtonYPressed = isCurrentPressed;
        }
    }


    // ==========================================
    // 5. 계산 및 유틸리티
    // ==========================================
    private void CalculateComplementaryFilter(float gx, float gy, float ax, float ay, float az)
    {
        // [축 교체 및 방향 보정]
        float accelRoll = Mathf.Atan2(ax, az) * Mathf.Rad2Deg;
        float accelPitch = Mathf.Atan2(-ay, Mathf.Sqrt(ax * ax + az * az)) * Mathf.Rad2Deg;

        float gyroPitch = _calcPitch + (gy * Mathf.Rad2Deg * ArduinoDt);
        float gyroRoll = _calcRoll + (gx * Mathf.Rad2Deg * ArduinoDt);

        _calcPitch = (filterWeight * gyroPitch) + ((1 - filterWeight) * accelPitch);
        _calcRoll = (filterWeight * gyroRoll) + ((1 - filterWeight) * accelRoll);
        

        CurrentPitch = _calcPitch;
        CurrentPitch *= -1;
        CurrentRoll = _calcRoll;
    }

    private float MapValue(float value) => (value / 1023.0f) * 2.0f - 1.0f;

    private float ApplyDeadzone(float value)
    {
        if (Mathf.Abs(value) < deadZone) return 0f;
        return Mathf.Sign(value) * ((Mathf.Abs(value) - deadZone) / (1 - deadZone));
    }

    // 데이터 전송
    public void SendSerialData(string message)
    {
        if (IsConnected && serialPort != null && serialPort.IsOpen)
        {
            try { serialPort.WriteLine(message); }
            catch { }
        }
    }

    private IEnumerator AttemptConnectionCoroutine()
{
    // 이미 연결되었다면 종료
    if (IsConnected) yield break;

    int attempts = 0;
    
    // 모드에 따라 포트와 속도 설정
    if (useUsbMode)
    {
        CurrentPortName = usbPortName;
        CurrentBaudRate = usbBaudRate;
    }
    else
    {
        CurrentPortName = btPortName;
        CurrentBaudRate = btBaudRate;
    }

    while (!IsConnected && attempts < MaxConnectionAttempts)
    {
        attempts++;
        Debug.Log($"<color=yellow>아두이노 연결 시도 중... (시도: {attempts}/{MaxConnectionAttempts})</color>");
        
        try
        {
            // SerialPort 객체 재할당
            serialPort = new SerialPort(CurrentPortName, CurrentBaudRate);
            serialPort.ReadTimeout = 10;
            serialPort.Open();
            IsConnected = true;

            // 연결 성공 시 초기화
            _calcPitch = 0f; _calcRoll = 0f;
            Debug.Log($"<color=green>아두이노 연결 성공! [{CurrentPortName} @ {CurrentBaudRate}]</color>");
            yield break; // 성공 시 코루틴 종료

        }
        catch (System.Exception ex)
        {
            IsConnected = false;
            // 마지막 시도 후에도 실패하면 에러 출력
            if (attempts == MaxConnectionAttempts)
            {
                Debug.LogError($"<color=red>최대 시도 횟수({MaxConnectionAttempts}회) 초과. 연결 실패 ({CurrentPortName}): {ex.Message}</color>");
            }
        }
        if (!IsConnected)
        {
            yield return new WaitForSeconds(RetryDelay);
        }
    }
}
}