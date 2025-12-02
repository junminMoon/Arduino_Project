using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    void Awake()
    {
        // 씬이 로드되거나 게임이 시작될 때 TimeScale을 1로 강제 설정합니다.
        // 이는 이전 씬에서 TimeScale=0으로 넘어온 문제를 해결합니다.
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }

        // 이 오브젝트를 씬 전환 시 파괴되지 않게 하여, 한 번만 실행되게 할 수도 있습니다.
        DontDestroyOnLoad(gameObject); 
    }
}