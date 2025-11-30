using UnityEngine;

// 이 스크립트는 Rigidbody 컴포넌트가 반드시 필요합니다.
[RequireComponent(typeof(Rigidbody))]
public class CustomBallPhysics : MonoBehaviour
{
    void Start()
    {
        // 기본값 7을 100으로 늘려서 제한을 풉니다.
        GetComponent<Rigidbody>().maxAngularVelocity = 100f;
    }

    public float gravityScale = 25.0f;

    // 물리 계산을 위해 Rigidbody 컴포넌트를 저장할 변수
    private Rigidbody rb;

    // 게임 시작 시 한 번 호출됩니다.
    void Awake()
    {
        // 스크립트가 붙어있는 오브젝트의 Rigidbody 컴포넌트를 찾아 변수에 할당합니다.
        rb = GetComponent<Rigidbody>();
    }

    // 물리 엔진의 업데이트 주기에 맞춰 고정된 간격으로 호출됩니다.
    // 중력처럼 일정한 힘을 가할 때 사용하는 것이 좋습니다.
    void FixedUpdate()
    {
        // Rigidbody에 아래 방향으로 중력 힘을 가합니다.
        // ForceMode.Acceleration은 질량(mass)에 상관없이 일정한 가속도를 적용합니다.
        rb.AddForce(Vector3.down * gravityScale, ForceMode.Acceleration);
    }
}