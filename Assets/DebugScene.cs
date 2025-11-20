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
        buttonTest.text = "B1 : " + arduinoPackage.IsButton1Pressed + "\nB2 : " + arduinoPackage.IsButton2Pressed + "\nB3 : " + arduinoPackage.IsButton3Pressed + "\nB4 : " + arduinoPackage.IsButton4Pressed;
    }

    void OnApplicationQuit()
    {
        if (arduinoPackage != null)
        {
            arduinoPackage.Disconnect();
        }
    }
}
