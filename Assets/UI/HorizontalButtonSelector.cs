using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // UI 선택 기능을 위해 필요

public class HorizontalButtonSelector : MonoBehaviour
{
    // 인스펙터에서 버튼 3개를 연결할 배열
    public Button[] buttons; 
    
    // 현재 선택된 버튼의 인덱스 (0, 1, 2)
    private int currentIndex = 0;

    // 키를 한 번 눌렀을 때 여러 번 선택되는 것을 방지하는 쿨다운 설정
    private const float InputCooldown = 0.2f; 
    private float lastInputTime;
    private ArduinoPackage arduinoPackage;

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackage>();
        if (buttons.Length > 0)
        {
            UpdateSelectionVisuals(); // 씬 시작 시 시각적 상태 초기화
            buttons[currentIndex].Select(); // EventSystem을 통해 선택 상태로 만듭니다.
        }
    }

    void Update()
    {
        // 1. 방향키 입력 처리 (기존 로직 유지)
        HandleDirectionalInput();

        if (arduinoPackage != null) 
        {
            arduinoPackage.ReadSerialLoop();  
        }

        // 2. E 키 입력 처리 (새로운 기능)
        // E 키를 눌렀고, 버튼 배열이 비어있지 않은 경우
        if ((Input.GetKeyDown(KeyCode.E) || arduinoPackage.IsButtonAPressed) && buttons.Length > 0)
        {
            // 현재 선택된 버튼의 OnClick() 이벤트를 강제로 실행합니다.
            buttons[currentIndex].onClick.Invoke();
        }
    }
    
    // 선택 이동 처리 및 인덱스 업데이트
    private void HandleDirectionalInput()
    {
        if (Time.time < lastInputTime + InputCooldown)
        {
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        int newIndex = currentIndex;

        if (horizontalInput > 0.5f || arduinoPackage.JoyX > 0.5f)
        {
            newIndex++;
            lastInputTime = Time.time;
        }
        else if (horizontalInput < -0.5f || arduinoPackage.JoyX < -0.5f)
        {
            newIndex--;
            lastInputTime = Time.time;
        }

        // 인덱스를 배열 범위 내로 유지
        newIndex = Mathf.Clamp(newIndex, 0, buttons.Length - 1);

        // 인덱스가 변경되었다면 시각적 상태와 EventSystem 선택을 업데이트합니다.
        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            UpdateSelectionVisuals();
            buttons[currentIndex].Select();
        }
    }

    /**
     * @brief 현재 선택된 버튼과 나머지 버튼의 색상을 시각적으로 업데이트합니다.
     */
    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
        // 자식 오브젝트 중 "HighlightPanel" (혹은 지정한 이름)을 찾습니다.
            Transform highlight = buttons[i].transform.Find("HighlightPanel");

            if (highlight != null)
            {
                // 현재 선택된 버튼일 때만 하이라이트 패널을 눕니다.
                if (i == currentIndex)
                {
                    highlight.gameObject.SetActive(true);
                }
                else
                {
                    highlight.gameObject.SetActive(false);
                }
            }
        }
   }
}