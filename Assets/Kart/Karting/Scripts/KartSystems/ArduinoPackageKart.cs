using UnityEngine;
using System.IO.Ports;
using System.Globalization;
using System;


public class ArduinoPackageKart : MonoBehaviour
{
    // ==========================================
    // 1. 인스펙터 설정 (Inspector Settings)
    // ==========================================
    [Header("통신 설정 (Communication)")]
    [SerializeField] private string portName = "COM8";
    [SerializeField] private int baudRate = 9600;

    [Header("설정 (Settings)")]
    [SerializeField] private float filterWeight = 0.98f;
    [SerializeField] private float deadZone = 0.15f;


    // ==========================================
    // 2. 외부 공개 데이터 (Properties)
    // ==========================================
    public bool IsConnected { get; private set; }

    // [MPU6050] 가공된 기울기 (상보 필터 적용)
    public float CurrentPitch { get; private set; }
    public float CurrentRoll { get; private set; }

    // [MPU6050] 디버그용 RAW 데이터 (6축)
    public float RawGyroX { get; private set; }
    public float RawGyroY { get; private set; }
    public float RawGyroZ { get; private set; }
    public float RawAccelX { get; private set; }
    public float RawAccelY { get; private set; }
    public float RawAccelZ { get; private set; }

    // [Joystick]
    public float JoyX { get; private set; }
    public float JoyY { get; private set; }
    public bool IsJoyPressed { get; private set; }

    // [Buttons]
    public bool IsButtonXPressed { get; private set; }
    public bool IsButtonYPressed { get; private set; }
    public bool IsButtonBPressed { get; private set; }
    public bool IsButtonAPressed { get; private set; }
    public bool IsTouchPressed { get; private set; }

    // 내부 변수
    private SerialPort serialPort;


    // ==========================================
    // 3. 연결 및 해제 (Connection)
    // ==========================================
    public void Connect()
    {
        if (IsConnected) return; // 이미 연결되어 있으면 패스

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

    // 앱이 꺼질 때 자동으로 연결 해제 (안전장치)
    void OnApplicationQuit()
    {
        Disconnect();
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
    // 5. 데이터 처리 로직 (Parsing Logic)
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

            // RAW 데이터 저장 (디버그용)
            RawGyroX = gx; RawGyroY = gy; RawGyroZ = gz;
            RawAccelX = ax; RawAccelY = ay; RawAccelZ = az;

            // 상보 필터 계산
            CalculateComplementaryFilter(gx, gy, ax, ay, az);
        }
        catch { }
    }

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
            }
            catch { }
        }
    }

    private void ProcessButtons(string key, string state)
    {
        bool isPressed = (state == "1");
        if (key == "X") IsButtonXPressed = isPressed;
        else if (key == "Y") IsButtonYPressed = isPressed;
        else if (key == "B") IsButtonBPressed = isPressed;
        else if (key == "A") IsButtonAPressed = isPressed;
        else if (key == "T") IsTouchPressed = isPressed;
    }

    // ==========================================
    // 6. 계산 및 유틸리티
    // ==========================================
    private void CalculateComplementaryFilter(float gx, float gy, float ax, float ay, float az)
    {
        float accelRoll = Mathf.Atan2(ay, az) * Mathf.Rad2Deg;
        float accelPitch = Mathf.Atan2(-ax, Mathf.Sqrt(ay * ay + az * az)) * Mathf.Rad2Deg;

        float gyroPitch = CurrentPitch + (gx * Mathf.Rad2Deg * Time.deltaTime);
        float gyroRoll = CurrentRoll + (gy * Mathf.Rad2Deg * Time.deltaTime);

        CurrentPitch = (filterWeight * gyroPitch) + ((1 - filterWeight) * accelPitch);
        CurrentRoll = (filterWeight * gyroRoll) + ((1 - filterWeight) * accelRoll);
    }

    private float MapValue(float value) => (value / 1023.0f) * 2.0f - 1.0f;

    private float ApplyDeadzone(float value)
    {
        if (Mathf.Abs(value) < deadZone) return 0f;
        return Mathf.Sign(value) * ((Mathf.Abs(value) - deadZone) / (1 - deadZone));
    }

    // ==========================================
    // 7. 데이터 전송 (Send)
    // ==========================================
    public void SendSerialData(string message)
    {
        if (IsConnected && serialPort != null && serialPort.IsOpen)
        {
            try { serialPort.WriteLine(message); }
            catch (System.Exception ex) { Debug.LogWarning($"전송 실패: {ex.Message}"); }
        }
    }
}