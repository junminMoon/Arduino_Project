using UnityEngine;
using TMPro;

public class MazeGameManager : MonoBehaviour
{
    [Header("References")]
    public MazeGenerator mazeGenerator;       // 미로 생성 스크립트
    private ArduinoPackage arduinoPackage;     // 아두이노 통신 스크립트

    [Header("UI")]
    public GameObject clearPanel;             // 클리어 시 뜰 패널

    private bool isGameClear = false;

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();

        // 2. 게임 시작
        StartGame();
    }

    void StartGame()
    {
        isGameClear = false;
        clearPanel.SetActive(false); // 클리어 UI 숨기기

        if (mazeGenerator != null)
        {
            mazeGenerator.InitMaze(); // 미로 새로 생성
        }
    }

    void Update()
    {
        if (arduinoPackage == null || !arduinoPackage.IsConnected) return;

        // 아두이노 데이터 읽기 (필수)
        arduinoPackage.ReadSerialLoop();

        // --- 게임 클리어 상태일 때만 버튼 입력 확인 ---
        if (isGameClear)
        {
            // Y 버튼: 재시작
            if (arduinoPackage.IsButtonBDown)
            {
                Debug.Log("Restart Game");
                StartGame();
            }
        }
    }

    // GoalTrigger(도착지점)에서 호출할 함수
    public void OnGameClear()
    {
        if (isGameClear) return; // 이미 클리어했으면 중복 실행 방지

        isGameClear = true;
        Debug.Log("Game Clear!");

        // 축하 UI 띄우기
        if (clearPanel != null) clearPanel.SetActive(true);

        arduinoPackage.SendSerialData("S 4");
        arduinoPackage.SendSerialData("V 3");
    }
}