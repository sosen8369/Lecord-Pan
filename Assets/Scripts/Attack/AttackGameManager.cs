using UnityEngine;
using System.Threading.Tasks;
using System;

public class AttackGameManager : MonoBehaviour
{
    [SerializeField] private AttackRhythmController attackController;

    // 비동기 작업의 완료 상태와 결과값을 관리하는 객체입니다.
    private TaskCompletionSource<RhythmResult> completionSource;

    /// <summary>
    /// 턴 매니저가 공격을 시작할 때 호출하는 함수입니다.
    /// </summary>
    public Task<RhythmResult> PlayAttackAsync(AttackPatternData patternData)
    {
        // 새로운 작업 완료 신호기를 생성합니다.
        completionSource = new TaskCompletionSource<RhythmResult>();

        // 공격 종료 이벤트를 구독하고 컨트롤러를 실행합니다.
        attackController.OnAttackFinished += HandleAttackFinished;
        attackController.StartAttack(patternData);

        // 턴 매니저에게 작업 상태(Task)를 반환하여 대기하게 만듭니다.
        return completionSource.Task;
    }

    private void HandleAttackFinished(RhythmResult result)
    {
        // 이벤트 구독을 해제하여 메모리 누수를 막습니다.
        attackController.OnAttackFinished -= HandleAttackFinished;

        // 대기 중이던 턴 매니저에게 결과값을 전달하며 대기를 해제합니다.
        completionSource.SetResult(result);
    }
}