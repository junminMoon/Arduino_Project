using UnityEngine;
using System.IO.Ports;
using System.Globalization;
using System;

public class ArduinoPackage : MonoBehaviour
{
    // ==========================================
    // 1. 외부 공개 데이터 (Properties)
    // ==========================================
    public bool IsConnected { get; private set; }

    // [MPU6050] 기울기 및 RAW 데이터
    public float CurrentPitch { get; private set; }
    public float CurrentRoll { get; private set; }

    // [Joystick] 조이스틱 데이터
    public float JoyX { get; private set; }    // 0 ~ 1023
    public float JoyY { get; private set; }    // 0 ~ 1023
    public bool IsJoyPressed { get; private set; } // 조이스틱 버튼 눌림 여부

    // [Buttons] 버튼 상태 (X, Y, B, A)
    public bool IsButton1Pressed { get; private set; }
    public bool IsButton2Pressed { get; private set; }
    public bool IsButton3Pressed { get; private set; }
    public bool IsButton4Pressed { get; private set; }


    // ==========================================
    // 2. 내부 변수
    // ==========================================
    private SerialPort serialPort;
    private const float FilterWeight = 0.98f;


    // ==========================================
    // 3. 초기화 및 연결 관리
    // ==========================================
    [SerializeField] private string portName = "COM8";
    [SerializeField] private int baudRate = 9600;
    public void Connect()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50;
            serialPort.Open();
            IsConnected = true;
            Debug.Log($"<color=green>아두이노 연결 성공! ({portName})</color>");
        }
        catch (System.Exception ex)
        {
            IsConnected = false;
            Debug.LogError($"<color=red>아두이노 연결 실패: {ex.Message}</color>");
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
    // 4. 메인 루프 (외부에서 호출)
    // ==========================================
    public void ReadSerialLoop()
    {
        if (!IsConnected || serialPort == null || !serialPort.IsOpen) return;

        try
        {
            string rawData = serialPort.ReadLine();
            DispatchData(rawData);
        }
        catch (System.TimeoutException) { }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"데이터 오류: {ex.Message}");
        }
    }


    // ==========================================
    // 5. 데이터 분류 (Dispatcher)
    // ==========================================
    private void DispatchData(string data)
    {
        // 데이터 예시: "G ...", "J 512,512,0", "X 1"
        if (string.IsNullOrEmpty(data)) return;

        string[] parts = data.Split(' ');
        if (parts.Length < 2) return;

        string key = parts[0];
        string value = parts[1];

        switch (key)
        {
            case "G":
                ProcessMPU(value);
                break;
            case "J": // 조이스틱 데이터 처리
                ProcessJoystick(value);
                break;
            case "X": // 버튼 1, 2, 3, 4 각각 처리
            case "Y":
            case "B":
            case "A":
                ProcessButtons(key, value);
                break;
        }
    }


    // ==========================================
    // 6. 기능별 처리 함수 (Handlers)
    // ==========================================

    // [MPU6050]
    private void ProcessMPU(string csvData)
    {
        string[] values = csvData.Split(',');
        if (values.Length != 6) return;

        try
        {
            float gx = float.Parse(values[0], CultureInfo.InvariantCulture);
            float gy = float.Parse(values[1], CultureInfo.InvariantCulture);
            // float gz = float.Parse(values[2]); 

            float ax = float.Parse(values[3], CultureInfo.InvariantCulture);
            float ay = float.Parse(values[4], CultureInfo.InvariantCulture);
            float az = float.Parse(values[5], CultureInfo.InvariantCulture);

            CalculateComplementaryFilter(gx, gy, ax, ay, az);
        }
        catch { }
    }

    // [Joystick] J x,y,sw (예: "512,512,0")
    private void ProcessJoystick(string csvData)
    {
        string[] values = csvData.Split(',');
        if (values.Length == 3)
        {
            try
            {
                JoyX = float.Parse(values[0], CultureInfo.InvariantCulture);
                JoyY = float.Parse(values[1], CultureInfo.InvariantCulture);

                // 아두이노 INPUT_PULLUP: 0이 눌림(Low), 1이 안눌림(High)
                int sw = int.Parse(values[2], CultureInfo.InvariantCulture);
                IsJoyPressed = (sw == 0); // 0이면 true(눌림)로 변환
            }
            catch { }
        }
    }

    // [Buttons] X 1 (예: 키="X", 값="1")
    private void ProcessButtons(string key, string state)
    {
        // 아두이노 코드에서 눌렸을 때 "1", 안 눌렸을 때 "0"을 보내도록 수정했으므로:
        bool isPressed = (state == "1");

        if (key == "X") 
        {
            IsButton1Pressed = isPressed;
        }
        else if (key == "Y")
        {
            IsButton2Pressed = isPressed;
        }
        else if (key == "B")
        {
            IsButton3Pressed = isPressed;
        }
        else if (key == "A")
        {
            IsButton4Pressed = isPressed;
        }
    }


    // ==========================================
    // 7. 수학 계산 (Algorithm)
    // ==========================================
    private void CalculateComplementaryFilter(float gx, float gy, float ax, float ay, float az)
    {
        float accelRoll = Mathf.Atan2(ay, az) * Mathf.Rad2Deg;
        float accelPitch = Mathf.Atan2(-ax, Mathf.Sqrt(ay * ay + az * az)) * Mathf.Rad2Deg;

        float gyroPitch = CurrentPitch + (gx * Mathf.Rad2Deg * Time.deltaTime);
        float gyroRoll = CurrentRoll + (gy * Mathf.Rad2Deg * Time.deltaTime);

        CurrentPitch = (FilterWeight * gyroPitch) + ((1 - FilterWeight) * accelPitch);
        CurrentRoll = (FilterWeight * gyroRoll) + ((1 - FilterWeight) * accelRoll);
    }
}