using UnityEngine;

public class MazeTiltControllerTemp : MonoBehaviour
{
    public string portName = "COM8";
    public int baudRate = 9600;

    public float maxAngle = 30.0f;
    public float smoothSpeed = 5.0f;

    private ArduinoPackageTemp arduinoPackage;

    void Start()
    {
        arduinoPackage = new ArduinoPackageTemp(portName, baudRate);

        arduinoPackage.Connect();
    }

    void Update()
    {
        arduinoPackage.ReadSerialLoop();

        if (arduinoPackage.IsConnected)
        {
            float pitch = arduinoPackage.CurrentPitch;
            float roll = arduinoPackage.CurrentRoll;

            ApplyRotation(pitch, roll);
        }
    }

    void ApplyRotation(float pitch, float roll)
    {
        pitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);
        roll = Mathf.Clamp(roll, -maxAngle, maxAngle);

        Quaternion targetRotation = Quaternion.Euler(pitch, 0, roll);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    // 앱 종료 시 연결 해제 (필수)
    void OnApplicationQuit()
    {
        if (arduinoPackage != null)
        {
            arduinoPackage.Disconnect();
        }
    }
}