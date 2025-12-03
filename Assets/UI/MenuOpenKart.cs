using UnityEngine;
using UnityEngine.SceneManagement; // ResumeGameFromButton 함수에서 TimeScale=1일 때 씬 전환 기능을 사용할 경우 대비 (선택사항)

public class MenuOpenKart : MonoBehaviour
{
    // 인스펙터에 연결할 캔버스
    public GameObject targetCanvas;

    // 아두이노 패키지 참조
    private ArduinoPackageKart arduinoPackage;
    
    // 메뉴가 열렸을 때 버튼 이벤트를 처리할 수 있도록 public으로 선언
    public bool IsMenuOpen { get; private set; } = false;

    void Start()
    {
        // 씬에서 ArduinoPackage를 찾아서 연결합니다.
        arduinoPackage = FindObjectOfType<ArduinoPackageKart>();
    }

    void Update()
    {
        bool isArduinoButtonPressed = false;
        // 1. 아두이노 패키지가 연결되어 있다면 시리얼 통신을 읽고 버튼 상태를 확인합니다.
        if (arduinoPackage != null) 
        {   
            isArduinoButtonPressed = arduinoPackage.IsButtonXDown;
        }

        // 2. 토글 조건 확인: X 키 입력 또는 아두이노 버튼 입력
        if (Input.GetKeyDown(KeyCode.X) || isArduinoButtonPressed)
        {
            // ToggleMenu()는 버튼을 한 번 눌렀을 때만 실행됩니다.
            ToggleMenu(); 
        }
    }

    // =======================================================
    // 📢 메뉴 열기/닫기 및 시간 제어 함수 (핵심 수정 부분)
    // =======================================================

    public void ToggleMenu()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("MenuOpen 스크립트에 targetCanvas가 연결되지 않았습니다! 메뉴 토글 실패.");
            return;
        }
        
        // 현재 메뉴 상태를 반전시킵니다.
        IsMenuOpen = !targetCanvas.activeSelf;

        if (IsMenuOpen)
        {
            // 메뉴 열기: 캔버스를 켜고 게임 시간을 멈춥니다.
            targetCanvas.SetActive(true);
            Time.timeScale = 0f; // 🚨 게임 일시 정지
        }
        else
        {
            // 메뉴 닫기: 캔버스를 끄고 게임 시간을 재개합니다.
            targetCanvas.SetActive(false);
            Time.timeScale = 1f; // 🚨 게임 재개
        }
    }

    /**
     * @brief UI 버튼에 연결하여 게임을 재개하는 함수 (메뉴 내부 '계속하기' 버튼용)
     */
    public void ResumeGameFromButton()
    {
        if (targetCanvas != null && targetCanvas.activeSelf)
        {
            ToggleMenu(); // 닫기 로직을 재활용하여 캔버스를 끄고 시간을 1로 설정
        }
    }
}