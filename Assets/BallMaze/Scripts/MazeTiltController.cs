using UnityEngine;

public class MazeTiltController : MonoBehaviour
{
    public float maxAngle = 30.0f;
    public float smoothSpeed = 5.0f;

    private ArduinoPackage arduinoPackage;

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
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

        Quaternion targetRotation = Quaternion.Euler(roll, 0, pitch);

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