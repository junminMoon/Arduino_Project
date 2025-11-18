using System;
using UnityEngine;
using System.IO.Ports; // 시리얼 통신
using System.Globalization;
using JetBrains.Annotations; // 소수점(`.`) 처리를 위해

public class ArduinoPackage : MonoBehaviour
{
    [Header("시리얼 포트 설정")]
    [Tooltip("아두이노가 연결된 COM 포트 (예: COM5)")]
    private String portName = "COM7";

    [Tooltip("아두이노와 동일하게 맞춘 통신 속도")]
    private int baudRate = 9600;

    private SerialPort serialPort;
    public String portData;
    public bool isConnect;

    void Start()
    {
        serialPort = new SerialPort(portName, baudRate);  
    }
    // 시리얼 포트 연결 시도
    public void Connect()
    {
        try
        {
            serialPort.ReadTimeout = 25;
            serialPort.Open();
            Debug.Log($"<color=green>아두이노 연결 성공! ({portName})</color>"); // '연결 성공'
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"<color=red>아두이노 연결 실패: {ex.Message}</color>"); // '실패'
        }
    }

    public void Disconnect()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }

    void Update()
    {
        isConnect = serialPort.IsOpen;
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                // 아두이노가 보낸 문자열 한 줄을 읽음
                string data = serialPort.ReadLine();

                portData = data;
            }
            catch (System.TimeoutException)
            {
                // 타임아웃 로그는 원래 없었으므로 그대로 둡니다. (무시)
            }
            catch (System.Exception ex)
            {
                // "데이터 읽기 오류" 로그는 문제 발생 시 필요하므로 남겨둡니다.
                Debug.LogWarning($"데이터 읽기 오류: {ex.Message}");
            }
        }
    }


    
}
