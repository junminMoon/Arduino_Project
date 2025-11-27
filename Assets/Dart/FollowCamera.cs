using UnityEngine;
using System.Collections;
using TMPro;

public class FollowCamera : MonoBehaviour
{
    [Header("카메라 설정")]
    public float followDistance = 3.0f;     // 다트와의 거리
    public float followHeight = 1.0f;       // 다트보다 얼마나 위에 위치할지
    public float smoothSpeed = 5f;          // 부드러운 이동 속도
    public float missFollowDuration = 2.0f; // 과녁 미적중 시 따라가는 시간

    [Header("점수판 설정")]
    public TextMeshProUGUI scoreText;          // 점수 표시 UI (Text 컴포넌트 포함)
    public float scoreDisplayDuration = 3.0f; // 점수 표시 시간

    private Transform targetDart;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isFollowing = false;
    private bool isScoring = false;

    void Start()
    {
        // 카메라의 초기 위치와 회전을 저장 (다시 돌아올 위치)
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // 점수 UI는 시작 시 숨김
        if(scoreText != null) scoreText.text = " ";
    }

    void Update()
    {
        if (isFollowing && targetDart != null)
        {
            // 다트가 바라보는 방향으로 카메라 위치 계산
            Vector3 desiredPosition = targetDart.position - targetDart.forward * followDistance + Vector3.up * followHeight;

            // 부드러운 이동
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // 다트를 부드럽게 바라보게 회전
            Quaternion targetRotation = Quaternion.LookRotation(targetDart.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 다트 발사 시 호출되어 카메라 팔로우를 시작합니다.
    /// </summary>
    public void StartFollowing(Transform dartTransform)
    {
        targetDart = dartTransform;
        isFollowing = true;
        isScoring = false;
        
        // 다트가 목표에 맞지 않았을 경우를 대비해 타이머 코루틴 시작
        StartCoroutine(MissCheckTimer());
    }

    /// <summary>
    /// 과녁에 맞지 않고 일정 시간이 지나면 카메라를 원래 위치로 복귀시킵니다.
    /// </summary>
    IEnumerator MissCheckTimer()
    {
        // missFollowDuration 동안 대기
        yield return new WaitForSeconds(missFollowDuration);

        // 만약 이 시간 동안 목표에 맞았거나(isScoring == true) target이 null이 되었다면, 복귀 로직을 건너뜁니다.
        if (isFollowing && !isScoring)
        {
            StopFollowing();
        }
    }
    
    /// <summary>
    /// 과녁에 명중했을 때 호출되어 점수를 표시합니다.
    /// </summary>
    public void HitTarget(int score)
    {
        if (!isScoring) // 이미 처리 중이 아니라면
        {
            isScoring = true;
            isFollowing = true; // 명중했으므로 계속 따라가서 박힌 장면을 보여줌
            StopCoroutine(MissCheckTimer()); // 미스 타이머 취소
            
            // 점수 표시 코루틴 시작
            StartCoroutine(DisplayScoreAndReset(score));
        }
    }

    /// <summary>
    /// 점수를 표시하고 카메라를 원래 위치로 복귀시킵니다.
    /// </summary>
    IEnumerator DisplayScoreAndReset(int score)
    {
        // 점수판에 점수 표시
        if (scoreText != null)
        {
            // 점수 텍스트 업데이트 로직 (UI Text 컴포넌트에 맞게 수정 필요)
            scoreText.text = "Score: " + score.ToString();
            Debug.Log($"Dart Hit! Score: {score}"); 
        }

        // 점수 표시 시간만큼 대기
        yield return new WaitForSeconds(scoreDisplayDuration);

        // 카메라 복귀
        StopFollowing();
    }
    
    /// <summary>
    /// 팔로우를 멈추고 카메라를 원래 위치로 복귀시킵니다.
    /// </summary>
    public void StopFollowing()
    {
        isFollowing = false;
        isScoring = false;
        targetDart = null;

        // 카메라를 원래 위치와 회전으로 즉시 복귀
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        
        // 점수판 숨김
        if(scoreText != null) scoreText.text = " ";
    }
}