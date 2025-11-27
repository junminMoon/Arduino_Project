using UnityEngine;

public class StickyDart : MonoBehaviour
{
    // === 기존 코드 유지 ===
    private Rigidbody rb;
    private bool isStuck = false;
    
    // === 추가된 변수 ===
    private FollowCamera followCamera; // FollowCamera 스크립트 참조

    // 타겟 태그 설정 (과녁 태그를 "Dartboard"로 설정했다고 가정)
    private const string TargetTag = "Dartboard"; 
    // 미적중 후 다트 오브젝트가 제거될 시간
    private float destroyTimeOnMiss = 0.5f; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 씬에서 FollowCamera 스크립트를 찾음
        followCamera = FindObjectOfType<FollowCamera>(); 
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck) return;

        if (collision.gameObject.CompareTag(TargetTag))
        {
            // 🎯 명중!
            StickToTarget(collision);
            
            // 1. 점수 계산 (간단하게 100점이라 가정)
            int calculatedScore = CalculateScore(collision.contacts[0].point); 
            
            // 2. 카메라에 명중 알림 전달
            if (followCamera != null)
            {
                followCamera.HitTarget(calculatedScore);
            }
        }
        else
        {
            isStuck = true;
            rb.isKinematic = true; 
            rb.velocity = Vector3.zero;
            
            // 미스했으므로 카메라 팔로우는 MissCheckTimer에 의해 자동으로 멈춥니다.
            
            // 충돌 후 다트 오브젝트 제거 예약 (메모리 관리)
            Destroy(gameObject, destroyTimeOnMiss);
            
            // 만약 타이머 만료 전에 다트가 파괴되면 카메라를 즉시 멈춥니다.
            if (followCamera != null)
            {
                // (선택 사항: 다트가 사라진 후 카메라 복귀를 더 빠르게 할 경우)
                // followCamera.StopFollowing(); 
            }
        }
    }

    void StickToTarget(Collision collision)
    {
        isStuck = true;
        rb.isKinematic = true; 
        rb.velocity = Vector3.zero;
        transform.SetParent(collision.transform);
        
        // 명중 시 다트는 카메라 복귀 후에 파괴됨 (FollowCamera가 다트 파괴 시점을 통제하도록 할 수도 있습니다.)
    }
    
    // 임시 점수 계산 로직 (과녁 중앙과의 거리에 따라 계산하는 로직으로 대체 필요)
    int CalculateScore(Vector3 impactPoint)
    {
        // 간단한 예시: 항상 100점 반환
        return 100;
    }
}