using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class DebugScene : MonoBehaviour
{
    // Start is called before the first frame update
    ArduinoPackage arduinoPackage;
    
    public TextMeshProUGUI joystickTest;
    public TextMeshProUGUI buttonTest;
    public TextMeshProUGUI touchTest;
    void Start()
    {
        arduinoPackage = new ArduinoPackage();
        arduinoPackage.Connect();
    }

    // Update is called once per frame
    void Update()
    {
        arduinoPackage.ReadSerialLoop();
        joystickTest.text = "JoyX : " + arduinoPackage.JoyX + "\nJoyY : " + arduinoPackage.JoyY + "\nJoyPressed : " + arduinoPackage.IsJoyPressed;
        buttonTest.text = "X : " + arduinoPackage.IsButtonXPressed + "\nY : " + arduinoPackage.IsButtonYPressed + "\nB : " + arduinoPackage.IsButtonBPressed + "\nA : " + arduinoPackage.IsButtonAPressed;
        touchTest.text = "Touch : " + arduinoPackage.IsTouchPressed;
    }

    void OnApplicationQuit()
    {
        if (arduinoPackage != null)
        {
            arduinoPackage.Disconnect();
        }
    }
}
