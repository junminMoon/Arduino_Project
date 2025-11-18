using UnityEngine;
using System.IO.Ports; // 시리얼 통신
using System.Globalization; // 소수점(`.`) 처리를 위해

public class MazeTiltController : MonoBehaviour
{
    [Header("시리얼 포트 설정")]
    [Tooltip("아두이노가 연결된 COM 포트 (예: COM5)")]
    public string portName = "COM7";
    [Tooltip("아두이노와 동일하게 맞춘 통신 속도")]
    public int baudRate = 9600; // ◀️ 아두이노와 9600으로 맞췄는지 확인!

    [Header("미로 설정")]
    [Tooltip("미로가 기울어질 최대 각도")]
    public float maxAngle = 30.0f;
    [Tooltip("값이 부드러워지는 속도. 높을수록 반응이 빨라짐.")]
    public float smoothSpeed = 5.0f;

    private SerialPort serialPort;
    private float currentPitch = 0.0f;
    private float currentRoll = 0.0f;

    // 게임이 시작될 때 1번만 호출
    void Start()
    {
        // 시리얼 포트 연결 시도
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 25;
            serialPort.Open();
            Debug.Log($"<color=green>아두이노 연결 성공! ({portName})</color>"); // ◀️ '연결 성공' 로그는 남겨두는 게 좋습니다!
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"<color=red>아두이노 연결 실패: {ex.Message}</color>"); // ◀️ '실패' 로그도 남겨둡니다.
        }
    }

    // 매 프레임마다 호출
    void Update()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                // 아두이노가 보낸 문자열 한 줄을 읽음
                string data = serialPort.ReadLine();

                // --- 'Debug.Log("Arduino RAW: " + data);' 라인 제거됨 ---

                // 데이터 파싱 및 회전 적용
                ParseAndRotate(data);
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

    // 아두이노가 보낸 문자열을 해석하고 미로를 회전시키는 함수
    void ParseAndRotate(string data)
    {
        // 예시 데이터: "P:15.2,R:-30.5"
        string[] parts = data.Split(',');
        foreach (string part in parts)
        {
            string[] kv = part.Split(':');
            if (kv.Length != 2) continue;

            string key = kv[0];
            string value = kv[1];

            try
            {
                // 소수점은 '.'으로 고정 (InvariantCulture)
                if (key == "P")
                {
                    currentPitch = float.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (key == "R")
                {
                    currentRoll = float.Parse(value, CultureInfo.InvariantCulture);
                }
            }
            catch (System.Exception) { /* 파싱 오류 무시 */ }
        }

        // 2. 회전 값 제한
        currentPitch = Mathf.Clamp(currentPitch, -maxAngle, maxAngle);
        currentRoll = Mathf.Clamp(-currentRoll, -maxAngle, maxAngle);

        // 3. 목표 각도 계산
        Quaternion targetRotation = Quaternion.Euler(currentPitch, 0, currentRoll);

        // --- 'Debug.Log($"Pitch: {currentPitch}, Roll: {currentRoll}");' 라인 제거됨 ---

        // 4. 실제 오브젝트에 '부드럽게' 회전 값 적용
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    // 프로그램 종료 시 포트 닫기
    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}