using UnityEngine;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    [Header("Dependencies")]
    // 인터페이스 대신 팀원 문서에 명시된 클래스 타입으로 직접 연결합니다.
    [SerializeField] private RhythmGameManager rhythmManager; 
    [SerializeField] private CommandUIManager _commandUIManager;

    [Header("Party Management")]
    public List<BattleUnit> playerParty = new List<BattleUnit>();
    public List<BattleUnit> enemyParty = new List<BattleUnit>();

    // 팀원 명세서에 맞춘 강제 종료 신호 토큰 관리 객체
    private CancellationTokenSource turnCts; 



    [Header("Camera Settings")]
    public Camera mainCamera; // 씬의 메인 카메라
    public Transform defenseCameraView; // 4패드 방어 시 바라볼 카메라의 위치/각도 (빈 오브젝트로 씬에 배치)
    
    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;

    private async void Start()
    {
        await StartBattleLoop();
    }

    private void Awake()
    {
        // 시작할 때 원래 카메라의 위치와 각도를 기억해둡니다.
        if (mainCamera != null)
        {
            _originalCamPos = mainCamera.transform.position;
            _originalCamRot = mainCamera.transform.rotation;
        }
    }

    private async Awaitable MoveCameraAsync(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        if (mainCamera == null) return;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 자연스러운 감속(Ease-out) 연출을 위한 SmoothStep 공식
            t = t * t * (3f - 2f * t); 

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            await Awaitable.NextFrameAsync(); // 다음 프레임까지 대기
        }

        // 오차 보정을 위해 마지막에 정확한 목표값으로 고정
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
    }


    private async Awaitable StartBattleLoop()
    {
        Debug.Log("=== 전투 시작 ===");
        while (!CheckBattleEndCondition())
        {
            await PlayerPhase();
            if (CheckBattleEndCondition()) break;

            await EnemyPhase();
            if (CheckBattleEndCondition()) break;
        }
    }

    // --- 플레이어 페이즈 ---
    private async Awaitable PlayerPhase()
    {
        Debug.Log("\n[플레이어 페이즈 시작]");
        foreach (var currentAttacker in playerParty)
        {
            if (currentAttacker == null || currentAttacker.IsDead) continue;

            // 1. 턴 시작 연출
            currentAttacker.SetFocus(true);

            try
            {
                // 2. 행동 선택 대기
                CommandType selectedCommand = await _commandUIManager.WaitForCommandAsync(currentAttacker);
                
                // 3. 타겟 선택 대기
                BattleUnit target = await WaitForPlayerTargetSelection(currentAttacker);

                if (target != null)
                {
                    // 4. 리듬 게임 실행 및 데미지 적용
                    await ExecuteAttackTurn(currentAttacker, target, selectedCommand);
                }
            }
            finally
            {
                // 5. 턴 종료 연출 복구 (에러가 나도 무조건 실행됨)
                currentAttacker.SetFocus(false);
            }

            if (CheckBattleEndCondition()) return;
        }
    }

    // --- 타겟 클릭 대기 ---
    private async Awaitable<BattleUnit> WaitForPlayerTargetSelection(BattleUnit attacker)
    {
        Debug.Log("타겟(적)을 마우스로 클릭하세요...");
        var completionSource = new AwaitableCompletionSource<BattleUnit>();

        void OnEnemyClicked(BattleUnit selectedUnit)
        {
            if (enemyParty.Contains(selectedUnit))
            {
                completionSource.SetResult(selectedUnit);
            }
        }

        TargetClickable.OnTargetClicked += OnEnemyClicked;
        BattleUnit target = await completionSource.Awaitable;
        TargetClickable.OnTargetClicked -= OnEnemyClicked;

        return target;
    }

    // --- [핵심] 팀원 명세서 기반 공격 로직 ---
    private async Awaitable ExecuteAttackTurn(BattleUnit attacker, BattleUnit target, CommandType command)
    {
        string chartId = "Test Battle Track";
        float skillMultiplier = (command == CommandType.Skill) ? 1.5f : 1.0f;
        
        /*
        turnCts = new CancellationTokenSource();

        try
        {
            // 리듬 게임 실행 및 결과 대기
            RhythmResult result = await rhythmManager.PlayRhythmGameAsync(chartId, true, turnCts.Token);

            // 결과 데이터를 바탕으로 전투 로직 수행
            Debug.Log($"게임 종료! 최종 정확도: {result.totalAccuracy}, 최대 콤보: {result.maxCombo}");
            
            // 단순화된 데미지 공식 적용
            float baseDamage = 20f * skillMultiplier;
            float finalDamage = baseDamage * result.totalAccuracy;
            target.TakeDamage(finalDamage);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("캐릭터 상태 이상 등으로 인해 리듬 게임 공격이 강제 취소되었습니다.");
        }
        finally
        {
            // 정상 종료든 강제 취소든 턴이 끝나면 토큰을 비워줍니다.
            turnCts?.Dispose();
            turnCts = null;
        }
        
        */
        await Awaitable.WaitForSecondsAsync(0.5f); // 턴 전환 전 짧은 대기
    }

    // --- 적군 페이즈 ---
    private async Awaitable EnemyPhase()
    {
        Debug.Log("\n[적군 페이즈 시작]");
        foreach (var currentEnemy in enemyParty)
        {
            if (currentEnemy == null || currentEnemy.IsDead) continue;

            // 적은 살아있는 첫 번째 플레이어를 자동 타겟팅
            BattleUnit targetPlayer = playerParty.FirstOrDefault(u => u != null && !u.IsDead);
            if (targetPlayer != null)
            {
                await ExecuteEnemyTurn(currentEnemy, targetPlayer);
            }

            if (CheckBattleEndCondition()) return;
        }
    }

    // --- 적군 방어 리듬 게임 로직 ---
    private async Awaitable ExecuteEnemyTurn(BattleUnit attacker, BattleUnit target)
    {
       string chartId = "Test Battle Track"; 
        turnCts = new CancellationTokenSource();

        try
        {
            Debug.Log("<color=red>[시스템] 적 공격! 방어 시점으로 카메라 이동!</color>");
            
            // ★ 아군 전체를 뒷모습으로 전환 명령
            foreach (var player in playerParty)
            {
                if (player != null && !player.IsDead) player.SetBackView(true);
            }

            // 방어용 카메라 앵글로 부드럽게 이동
            if (defenseCameraView != null)
            {
                await MoveCameraAsync(defenseCameraView.position, defenseCameraView.rotation, 0.5f);
            }

            // 4패드 리듬 게임 실행
            RhythmResult result = await rhythmManager.PlayRhythmGameAsync(chartId, false, turnCts.Token);
            
            float baseDamage = 30f;
            float damageTaken = baseDamage * (1f - result.totalAccuracy); 
            
            target.TakeDamage(damageTaken);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("적 턴(방어) 리듬 게임이 강제 취소되었습니다.");
        }
        finally
        {
            turnCts?.Dispose();
            turnCts = null;

            // 원래 시점으로 카메라 복귀
            Debug.Log("<color=cyan>[시스템] 원래 시점으로 카메라 복귀</color>");
            await MoveCameraAsync(_originalCamPos, _originalCamRot, 0.5f);

            // ★ 카메라가 돌아왔으므로 아군 전체를 다시 앞모습으로 전환 명령
            foreach (var player in playerParty)
            {
                if (player != null && !player.IsDead) player.SetBackView(false);
            }
        }

        await Awaitable.WaitForSecondsAsync(0.5f);
    }

    /// <summary>
    /// 외부 시스템에서 턴을 강제로 끝낼 때 호출하는 함수
    /// </summary>
    public void CancelCurrentTurn()
    {
        if (turnCts != null)
        {
            turnCts.Cancel(); // 토큰 취소 신호 발송
        }
    }

    private bool CheckBattleEndCondition()
    {
        if (playerParty.All(u => u == null || u.IsDead))
        {
            Debug.Log("=== 패배: 아군 전멸 ===");
            return true;
        }
        if (enemyParty.All(u => u == null || u.IsDead))
        {
            Debug.Log("=== 승리: 적군 전멸 ===");
            return true;
        }
        return false;
    }
}