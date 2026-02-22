using System;
using System.Threading;
using UnityEngine;

public class RhythmGameManager : MonoBehaviour, IRhythmSystem
{
    [Header("UI References")]
    [SerializeField] private GameObject rhythmUICanvas;

    [Header("Sub Managers")]
    [SerializeField] private RhythmLogicController rhythmLogic; 

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
        using (cancellationToken.Register(() => 
        {
            AbortGame(); 
            completionSource.SetException(new OperationCanceledException(cancellationToken));
        }))
        {
            // 3. 정상 종료 시 결과를 받아올 로컬 이벤트 콜백 정의
            Action<RhythmResult> onGameEnd = null;
            onGameEnd = (result) => 
            {
                rhythmLogic.OnGameFinished -= onGameEnd; // 메모리 누수 방지를 위한 구독 해제
                
                // ★ 핵심 버그 픽스: 정상 종료 시에도 반드시 내부 데이터를 청소해야 다음 적의 턴에 노트가 제대로 내려옵니다.
                rhythmLogic.StopAllAudio();
                rhythmLogic.ReturnAllObjectsToPool();
                rhythmLogic.ResetSessionData();

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
        rhythmLogic.StopAllAudio();
        rhythmLogic.ReturnAllObjectsToPool();
        rhythmLogic.ResetSessionData();
        rhythmUICanvas.SetActive(false);
    }

    public void PauseRhythm()
    {
        rhythmLogic.PauseSystem();
    }

    public void ResumeRhythm()
    {
        rhythmLogic.ResumeSystem();
    }
}