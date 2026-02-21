using UnityEngine;

public class AttackTestRunner : MonoBehaviour
{
    [SerializeField] private AttackRhythmController attackController;
    [SerializeField] private AttackPatternData testPattern; // SO 할당용 변수

    private void Start()
    {
        if (testPattern == null)
        {
            Debug.LogError("테스트할 패턴 에셋(SO)이 할당되지 않았습니다.");
            return;
        }

        attackController.OnAttackFinished += HandleAttackFinished;
        attackController.StartAttack(testPattern);
    }

    private void HandleAttackFinished(RhythmResult result)
    {
        Debug.Log($"공격 종료! 최종 정확도(데미지 배율): {result.totalAccuracy}");
        attackController.OnAttackFinished -= HandleAttackFinished;
    }
}