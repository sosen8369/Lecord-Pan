using System;
using System.Threading;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    // 팀원의 매니저를 이 인터페이스로 참조합니다.
    [SerializeField] private RhythmGameManager _rhythmManager; 
    
    // 현재 턴의 취소를 관리할 토큰
    private CancellationTokenSource _currentTurnCTS;

    // --- 1. 턴 진행 로직 ---
    public async Awaitable ProcessPlayerTurn()
    {
        Debug.Log("플레이어 턴 시작: 공격 행동 선택됨");
        string chartId = "Attack_01";
        
        // 새로운 턴이 시작될 때마다 토큰을 새로 발급해야 합니다.
        _currentTurnCTS = new CancellationTokenSource();

        try
        {
            // 리듬 게임 시작 명령을 내리고 대기합니다. (토큰 전달)
            RhythmResult result = await _rhythmManager.PlayRhythmGameAsync(chartId, true, _currentTurnCTS.Token);

            // [중요] 이 줄에 도달했다는 것은 리듬 게임이 '무사히 끝났다'는 뜻입니다.
            Debug.Log($"공격 성공! 정확도: {result.totalAccuracy * 100}%");
            ApplyDamageToEnemy(result.totalAccuracy);
        }
        catch (OperationCanceledException)
        {
            // 팀원이 던진 예외를 여기서 받아냅니다.
            // 중간에 포기했거나 HP가 0이 되어 취소된 경우 무조건 이쪽으로 빠집니다.
            Debug.LogWarning("리듬 게임 강제 종료 감지. 데미지 계산을 건너뜁니다.");
            ProcessGameOver(); // 또는 턴 강제 종료 로직
        }
        finally
        {
            // 메모리 누수를 막기 위해 사용이 끝난 토큰은 반드시 해제(Dispose)합니다.
            _currentTurnCTS?.Dispose();
            _currentTurnCTS = null;
        }
    }

    // --- 2. 턴 강제 종료 제어 ---
    // 캐릭터의 HP가 0이 되거나, 유저가 '포기' 버튼을 눌렀을 때 당신이 호출할 함수
    public void AbortCurrentTurn()
    {
        if (_currentTurnCTS != null && !_currentTurnCTS.IsCancellationRequested)
        {
            Debug.Log("턴 매니저 -> 리듬 매니저로 강제 취소 신호 송신");
            _currentTurnCTS.Cancel(); // 이 함수를 부르는 순간 팀원의 루프가 정지하고 예외가 발생합니다.
        }
    }

    // --- 3. 일시정지 제어 ---
    public void TogglePause(bool isPause)
    {
        if (isPause)
        {
            ShowPauseUI(); // 턴 매니저의 일시정지 캔버스 띄우기
            _rhythmManager.PauseRhythm(); // 팀원 시스템 시간 정지 명령
        }
        else
        {
            HidePauseUI();
            _rhythmManager.ResumeRhythm();
        }
    }

    private void ApplyDamageToEnemy(float accuracy) { /* 데미지 연산 로직 */ }
    private void ProcessGameOver() { /* 패배 처리 로직 */ }
    private void ShowPauseUI() { /* UI 로직 */ }
    private void HidePauseUI() { /* UI 로직 */ }
}