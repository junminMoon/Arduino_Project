using UnityEngine;
public class CustomBallPhysics : MonoBehaviour
{
    private Rigidbody rb;

    // 공이 위로 튀어 오를 수 있는 최대 속도 제한
    public float maxUpwardSpeed = 2.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 100f; // (기존 기능: 회전 속도 제한 해제)
    }

    public float gravityScale = 25.0f;

    void FixedUpdate()
    {
        // 공이 위쪽(Y축)으로 너무 빨리 움직이려고 하면?
        if (rb.velocity.y > maxUpwardSpeed)
        {
            // 강제로 속도를 깎아버림 (눌러주는 효과)
            Vector3 newVel = rb.velocity;
            newVel.y = maxUpwardSpeed;
            rb.velocity = newVel;
        }
        rb.AddForce(Vector3.down * gravityScale, ForceMode.Acceleration);
    }
}