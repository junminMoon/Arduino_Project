using UnityEngine;
using System.IO.Ports; // 시리얼 통신
using System.Globalization;
using UnityEditor;
using Unity.VisualScripting; // 소수점(`.`) 처리를 위해

public class MazeTiltController : MonoBehaviour
{
    [Header("미로 설정")]
    [Tooltip("미로가 기울어질 최대 각도")]
    public float maxAngle = 30.0f;
    [Tooltip("값이 부드러워지는 속도. 높을수록 반응이 빨라짐.")]
    public float smoothSpeed = 5.0f;
    private ArduinoPackage arduinoPackage;
    private float currentPitch = 0.0f;
    private float currentRoll = 0.0f;

    // 게임이 시작될 때 1번만 호출
    void Start()
    {
        arduinoPackage = new ArduinoPackage();
        arduinoPackage.Connect();
    }

    void Update()
    {
        if(arduinoPackage.isConnect)
        {
            ParseAndRotate(arduinoPackage.portData);  
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
        arduinoPackage.Disconnect();
    }
}