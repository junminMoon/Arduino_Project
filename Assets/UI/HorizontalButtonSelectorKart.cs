using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic; // List를 사용하지 않아도 되지만, 표준 using은 유지합니다.

public class HorizontalButtonSelectorKart : MonoBehaviour
{
    // 인스펙터에서 버튼 3개를 연결할 배열
    public Button[] buttons; 
    
    // 현재 선택된 버튼의 인덱스 (0, 1, 2)
    private int currentIndex = 0;

    // 키를 한 번 눌렀을 때 여러 번 선택되는 것을 방지하는 쿨다운 설정
    private const float InputCooldown = 0.2f; 
    // 🚨 TimeScale = 0에서도 작동하도록 Time.time -> Time.unscaledTime으로 변경합니다.
    private float lastInputTime; 
    
    private ArduinoPackageKart arduinoPackage;

    void Start()
    {
        arduinoPackage = FindObjectOfType<ArduinoPackageKart>();
        if (buttons.Length > 0)
        {
            UpdateSelectionVisuals(); // 씬 시작 시 시각적 상태 초기화
            buttons[currentIndex].Select(); // EventSystem을 통해 선택 상태로 만듭니다.
        }
    }

    void Update()
    {
        // 2. 방향 입력 처리 (선택 이동)
        HandleDirectionalInput();
        
        // 3. 버튼 클릭 입력 처리 (Null 체크 및 E 키/아두이노 A 버튼 통합)
        bool isEKartButtonPressed = Input.GetKeyDown(KeyCode.E);
        bool isArduinoAPressed = (arduinoPackage != null && arduinoPackage.IsButtonAPressed);

        if ((isEKartButtonPressed || isArduinoAPressed) && buttons.Length > 0)
        {
            // 현재 선택된 버튼의 OnClick() 이벤트를 강제로 실행합니다.
            buttons[currentIndex].onClick.Invoke();
        }
    }
    
    // 선택 이동 처리 및 인덱스 업데이트
    private void HandleDirectionalInput()
    {
        // 🚨 TimeScale = 0에서도 작동하도록 Time.time -> Time.unscaledTime으로 변경합니다.
        if (Time.unscaledTime < lastInputTime + InputCooldown)
        {
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float arduinoJoyX = (arduinoPackage != null) ? arduinoPackage.JoyX : 0f; // 🚨 Null 체크 후 JoyX 값 사용
        
        int newIndex = currentIndex;

        // 키보드 또는 아두이노 조이콘 입력 처리
        if (horizontalInput > 0.5f || arduinoJoyX > 0.5f)
        {
            newIndex++;
            // 🚨 Time.unscaledTime으로 타이머 업데이트
            lastInputTime = Time.unscaledTime;
        }
        else if (horizontalInput < -0.5f || arduinoJoyX < -0.5f)
        {
            newIndex--;
            // 🚨 Time.unscaledTime으로 타이머 업데이트
            lastInputTime = Time.unscaledTime;
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
     * @brief 현재 선택된 버튼과 나머지 버튼의 시각적 상태를 업데이트합니다.
     */
    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            // 자식 오브젝트 중 "HighlightPanel" (혹은 지정한 이름)을 찾습니다.
            Transform highlight = buttons[i].transform.Find("HighlightPanel");

            if (highlight != null)
            {
                // 현재 선택된 버튼일 때만 하이라이트 패널을 활성화합니다.
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