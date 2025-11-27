using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class DebugSceneKart : MonoBehaviour
{

    public TextMeshProUGUI joystickTest;
    public TextMeshProUGUI buttonTest;
    public TextMeshProUGUI touchTest;
    public TextMeshProUGUI gyroTest;

    private ArduinoPackageKart arduinoPackage;

    void Start()
    {
        if (arduinoPackage == null)
        {
            arduinoPackage = FindObjectOfType<ArduinoPackageKart>();
        }

        if (arduinoPackage != null)
        {
            arduinoPackage.Connect(); // 연결 시작!
        }
        else
        {
            Debug.LogError("씬에 'ArduinoPackageTEMP' 스크립트가 붙은 오브젝트가 없습니다!");
        }
    }

    void Update()
    {
        if (arduinoPackage == null) return;

        // 1. 데이터 읽기 명령 (Listener 방식)
        arduinoPackage.ReadSerialLoop();

        // 2. UI 텍스트 갱신
        if (arduinoPackage.IsConnected)
        {
            joystickTest.text = $"JoyX : {arduinoPackage.JoyX:F2}\nJoyY : {arduinoPackage.JoyY:F2}\nJoyPressed : {arduinoPackage.IsJoyPressed}";

            buttonTest.text = $"X : {arduinoPackage.IsButtonXPressed}\nY : {arduinoPackage.IsButtonYPressed}\nB : {arduinoPackage.IsButtonBPressed}\nA : {arduinoPackage.IsButtonAPressed}";

            touchTest.text = $"Touch : {arduinoPackage.IsTouchPressed}";

            // 6축 RAW 데이터 + 계산된 각도 표시
            gyroTest.text = $"Gyro\nX :{arduinoPackage.RawGyroX:F2}\nY : {arduinoPackage.RawGyroY:F2}\nZ : {arduinoPackage.RawGyroZ:F2}\n" +
                            $"Accel\nX : {arduinoPackage.RawAccelX:F2}\nY:{arduinoPackage.RawAccelY:F2}\nZ:{arduinoPackage.RawAccelZ:F2}\n" +
                            $"Angle\nPitch : {arduinoPackage.CurrentPitch:F1}\nRoll : {arduinoPackage.CurrentRoll:F1}\n";
        }
        else
        {
            gyroTest.text = "Disconnected...";
        }

        // 3. 소리 전송 테스트 (키보드 1~4)
        if (Input.GetKeyDown(KeyCode.Alpha1)) arduinoPackage.SendSerialData("S 1");
        if (Input.GetKeyDown(KeyCode.Alpha2)) arduinoPackage.SendSerialData("S 2");
        if (Input.GetKeyDown(KeyCode.Alpha3)) arduinoPackage.SendSerialData("S 3");
        if (Input.GetKeyDown(KeyCode.Alpha4)) arduinoPackage.SendSerialData("S 4");
    }

    public void OnClickSound(int soundId)
    {
        if (arduinoPackage != null && arduinoPackage.IsConnected)
        {
            string command = "S " + soundId;
            arduinoPackage.SendSerialData(command);

            Debug.Log($"[UI] 소리 버튼 클릭: {command}");
        }
        else
        {
            Debug.LogWarning("아두이노가 연결되지 않았습니다!");
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