using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugScene : MonoBehaviour
{
    [Header("Arduino Package Connection")]
    // 인스펙터에서 직접 연결하는 것을 권장합니다.
    [SerializeField] private ArduinoPackage arduinoPackage;

    [Header("UI Text References")]
    public TextMeshProUGUI joystickTest;
    public TextMeshProUGUI buttonTest;
    public TextMeshProUGUI touchTest;
    public TextMeshProUGUI gyroTest;
    public TextMeshProUGUI infoText; // ◀️ [추가] 연결 정보 표시용 텍스트

    void Start()
    {
        if (arduinoPackage == null)
        {
            arduinoPackage = FindObjectOfType<ArduinoPackage>();
        }
    }

    void Update()
    {
        if (arduinoPackage == null) return;

        // 2. UI 텍스트 갱신
        if (arduinoPackage.IsConnected)
        {
            // ---------------------------------------------------------
            // ★ [추가] 현재 연결 모드 및 포트 정보 표시
            // ---------------------------------------------------------
            string mode = arduinoPackage.useUsbMode ? "<color=yellow>[Wired USB]</color>" : "<color=yellow>[Wireless BT]</color>";
            infoText.text = $"{mode}\nPort: {arduinoPackage.CurrentPortName}\nBaud: {arduinoPackage.CurrentBaudRate}";


            // 기존 데이터 표시
            joystickTest.text = $"JoyX : {arduinoPackage.JoyX:F2}\nJoyY : {arduinoPackage.JoyY:F2}\nJoyPressed : {arduinoPackage.IsJoyPressed}";

            buttonTest.text = $"X : {arduinoPackage.IsButtonXPressed}\nY : {arduinoPackage.IsButtonYPressed}\nB : {arduinoPackage.IsButtonBPressed}\nA : {arduinoPackage.IsButtonAPressed}";

            touchTest.text = $"Touch : {arduinoPackage.IsTouchPressed}";

            // 6축 RAW 데이터 + 계산된 각도 표시
            gyroTest.text = $"Gyro\nX :{arduinoPackage.RawGyroX:F2}\nY : {arduinoPackage.RawGyroY:F2}\nZ : {arduinoPackage.RawGyroZ:F2}\n" +
                            $"Accel\nX : {arduinoPackage.RawAccelX:F2}\nY:{arduinoPackage.RawAccelY:F2}\nZ:{arduinoPackage.RawAccelZ:F2}\n" +
                            $"Angle\nPitch : {arduinoPackage.CurrentPitch:F1}\nRoll : {arduinoPackage.CurrentRoll:F1}\nYaw : {arduinoPackage.CurrentYaw:F1}";
        }
        else
        {
            infoText.text = "Status: <color=red>Disconnected</color>";
            gyroTest.text = ""; // 연결 끊기면 나머지 텍스트 비우기 (선택 사항)
            joystickTest.text = "";
            buttonTest.text = "";
            touchTest.text = "";
        }
    }

    public void OnClickSound(int soundId)   // soundId : 1 -> 띠띵(도미) 2 -> 띠(라) 3 -> 띠띠(솔솔) 4 -> 띠로리(도미솔)
    {
        if (arduinoPackage != null && arduinoPackage.IsConnected)
        {
            string command = "S " + soundId;
            arduinoPackage.SendSerialData(command);
            Debug.Log($"[UI] 소리 버튼 클릭: {command}");
        }
    }

    public void OnClickVibration(int vibId) // vibId : 1 -> 약한 진동 2 -> 강한 진동 3 -> 중간 진동 두번
    {
        if (arduinoPackage != null && arduinoPackage.IsConnected)
        {
            string command = "V " + vibId;
            arduinoPackage.SendSerialData(command);
            Debug.Log($"[Vibration Test] 전송함: {command}");
        }
    }

    void OnApplicationQuit()
    {
        if (arduinoPackage != null)
        {
            arduinoPackage.Disconnect();
        }
    }
}