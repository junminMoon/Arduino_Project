using UnityEngine;
using TMPro;

public class MazeTiltController : MonoBehaviour
{
    public float maxAngle = 30.0f;
    public float smoothSpeed = 10.0f;
    public bool isTiltMode = false;
    public TextMeshProUGUI modeText;
    float roll = 0;
    float pitch = 0;
    private ArduinoPackage arduinoPackage;

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
    }

    void Update()
    {
        if (arduinoPackage.IsConnected)
        {
            if (isTiltMode)
            {
                pitch = arduinoPackage.CurrentPitch;
                roll = arduinoPackage.CurrentRoll;
            }
            else
            {
                pitch = arduinoPackage.JoyX * 250;
                roll = arduinoPackage.JoyY * 250;
            }

            ApplyRotation(pitch, roll);
        }

        if (arduinoPackage.IsButtonYPressed)
        {
            if (isTiltMode == false)
            {
                isTiltMode = true;
            }
            else
            {
                isTiltMode = false;
            }
        }
        modeText.text = isTiltMode ? "Tilt" : "JoyStick";
    }

    void ApplyRotation(float pitch, float roll)
    {

        pitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);
        roll = Mathf.Clamp(roll, -maxAngle, maxAngle);

        Quaternion targetRotation = Quaternion.Euler(roll, 0, pitch);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}