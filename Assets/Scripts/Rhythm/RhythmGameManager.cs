using System;
using System.Threading;
using UnityEngine;

public class RhythmGameManager : MonoBehaviour, IRhythmSystem
{
    [Header("UI References")]
    [SerializeField] private GameObject rhythmUICanvas;

    [Header("Sub Managers")]
    [SerializeField] private RhythmLogicController rhythmLogic; 
    // 하위 시스템(오디오, 스포너, 판정 등)을 제어하는 래퍼 클래스 또는 개별 참조

    /// <summary>
    /// 턴 매니저가 호출할 리듬 게임 진입점입니다.
    /// </summary>
    public async Awaitable<RhythmResult> PlayRhythmGameAsync(string chartID, bool isAttackTurn, CancellationToken cancellationToken)
    {
        // 1. UI 활성화 및 내부 데이터 초기화
        rhythmUICanvas.SetActive(true);
        rhythmLogic.InitializeGameData(chartID, isAttackTurn);

        // 비동기 반환 처리를 위한 CompletionSource 생성
        AwaitableCompletionSource<RhythmResult> completionSource = new AwaitableCompletionSource<RhythmResult>();

        // 2. 강제 종료(Cancel) 신호 감지 콜백 등록
        // CancellationToken이 활성화되면 람다식 내부가 즉시 실행됩니다.
        using (cancellationToken.Register(() => 
        {
            AbortGame(); // 내부 초기화 및 정리 작업
            completionSource.SetException(new OperationCanceledException(cancellationToken));
        }))
        {
            // 3. 정상 종료 시 결과를 받아올 로컬 이벤트 콜백 정의
            Action<RhythmResult> onGameEnd = null;
            onGameEnd = (result) => 
            {
                rhythmLogic.OnGameFinished -= onGameEnd; // 메모리 누수 방지를 위한 구독 해제
                rhythmUICanvas.SetActive(false);         // UI 숨김
                completionSource.SetResult(result);      // 턴 매니저로 결과 반환 및 대기 해제
            };
            
            // 시스템 정상 종료 이벤트 구독
            rhythmLogic.OnGameFinished += onGameEnd;

            // 4. 실제 음악 재생 및 노트 스폰 루프 시작
            rhythmLogic.StartRhythmLoop();

            // 5. 결과가 반환되거나 예외(Cancel)가 던져질 때까지 비동기 대기
            return await completionSource.Awaitable;
        }
    }

    /// <summary>
    /// 플레이어 사망 등으로 인한 강제 종료 시 호출되는 정리 함수입니다.
    /// </summary>
    private void AbortGame()
    {
        // 진행 중인 모든 프로세스 강제 중단
        rhythmLogic.StopAllAudio();
        rhythmLogic.ReturnAllObjectsToPool();
        rhythmLogic.ResetSessionData();
        
        // 캔버스 비활성화
        rhythmUICanvas.SetActive(false);
    }

    /// <summary>
    /// 턴 매니저의 일시정지 메뉴에서 호출할 시간 제어 함수입니다.
    /// </summary>
    public void PauseRhythm()
    {
        rhythmLogic.PauseSystem();
    }

    public void ResumeRhythm()
    {
        rhythmLogic.ResumeSystem();
    }
}