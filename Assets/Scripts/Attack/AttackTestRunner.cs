using UnityEngine;

public class AttackTestRunner : MonoBehaviour
{
    [SerializeField] private AttackRhythmController attackController;
    
    // 예시: 1초, 2초, 3.5초 뒤에 타겟에 도달하는 3번의 공격
    [SerializeField] private float[] testTimings = new float[] { 1000f, 2000f, 3500f };

    private void Start()
    {
        // 결과 수신을 위한 이벤트 구독
        attackController.OnAttackFinished += HandleAttackFinished;
        
        // 공격 세션 시작
        attackController.StartAttack(testTimings);
    }

    private void HandleAttackFinished(RhythmResult result)
    {
        Debug.Log($"공격 종료! 최종 정확도(데미지 배율): {result.totalAccuracy}, 최대 콤보: {result.maxCombo}");
        
        // 테스트 종료 후 이벤트 구독 해제
        attackController.OnAttackFinished -= HandleAttackFinished;
    }
}