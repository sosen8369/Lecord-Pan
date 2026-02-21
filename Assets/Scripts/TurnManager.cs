using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject _rhythmManagerObject;
    private IRhythmSystem _rhythmManager;
    private CancellationTokenSource _currentTurnCTS;
    [Header("UI Managers")]
    [SerializeField] private CommandUIManager _commandUIManager; // 방금 만든 UI 매니저 연결

    [Header("Party Management")]
    // 1~4명까지 자유롭게 넣을 수 있습니다. 
    // 전투 중 교체하고 싶다면 이 리스트의 요소를 바꿔치기하면 됩니다.
    public List<BattleUnit> playerParty = new List<BattleUnit>();
    public List<BattleUnit> enemyParty = new List<BattleUnit>();


    private async void Start()
    {
        _rhythmManager = _rhythmManagerObject.GetComponent<IRhythmSystem>();
        await StartBattleLoop();
    }

    private async Awaitable StartBattleLoop()
    {
        Debug.Log("=== 페이즈 기반 전투 시작 ===");

        while (!CheckBattleEndCondition())
        {
            // 1. 플레이어 페이즈 (앞에서부터 순서대로)
            await PlayerPhase();
            if (CheckBattleEndCondition()) break;

            // 2. 적군 페이즈 (앞에서부터 순서대로)
            await EnemyPhase();
            if (CheckBattleEndCondition()) break;
        }
    }

    // --- 플레이어 페이즈 (1~4명 순차 진행) ---
    private async Awaitable PlayerPhase()
    {
        Debug.Log("\n[플레이어 페이즈 시작]");

        for (int i = 0; i < playerParty.Count; i++)
        {
            BattleUnit currentAttacker = playerParty[i];

            if (currentAttacker == null || currentAttacker.IsDead) continue;

            Debug.Log($"--- {currentAttacker.unitName}의 행동 차례 ---");

            // 1. [연출] 캐릭터 튀어나오고 스포트라이트 ON
            currentAttacker.SetFocus(true);

            // 2. [행동 선택] 캐릭터 옆에 UI 띄우고 버튼 누를 때까지 무한 대기
            CommandType selectedCommand = await _commandUIManager.WaitForCommandAsync(currentAttacker);
            Debug.Log($"{currentAttacker.unitName}가 {selectedCommand} 행동을 선택했습니다.");

            // 향후 '취소(Cancel)' 버튼 추가 시 이전 캐릭터로 돌아가는 로직도 여기에 붙일 수 있습니다.

            // 3. [타겟 선택] 타겟 마우스 클릭 대기
            BattleUnit target = await WaitForPlayerTargetSelection(currentAttacker);

            if (target != null)
            {
                // 4. [실행] 리듬 게임 및 데미지 적용
                // 선택한 행동이 스킬인지 기본 공격인지에 따라 넘겨주는 채보(ChartID)를 다르게 할 수 있습니다.
                await ExecutePlayerAttack(currentAttacker, target, selectedCommand);
            }

            // 5. [연출 복구] 턴 종료 시 제자리로 돌아가고 스포트라이트 OFF
            currentAttacker.SetFocus(false);

            if (CheckBattleEndCondition()) return;
        }
    }
    private async Awaitable ExecutePlayerAttack(BattleUnit attacker, BattleUnit target, CommandType command) 
    {
        // command 값에 따라 chartId를 "Player_Attack_01"로 할지 "Player_Skill_01"로 할지 분기 처리 가능
        // ... 기존 로직 ...
    }

    // --- 적군 페이즈 ---
    private async Awaitable EnemyPhase()
    {
        Debug.Log("\n[적군 페이즈 시작]");

        for (int i = 0; i < enemyParty.Count; i++)
        {
            BattleUnit currentEnemy = enemyParty[i];

            if (currentEnemy == null || currentEnemy.IsDead) continue;

            // 적의 타겟팅 AI (단순히 살아있는 첫 번째 아군을 타겟으로 잡음)
            BattleUnit targetPlayer = playerParty.FirstOrDefault(u => u != null && !u.IsDead);

            if (targetPlayer != null)
            {
                await ExecuteEnemyAttack(currentEnemy, targetPlayer);
            }

            if (CheckBattleEndCondition()) return;
        }
    }

    // --- 타겟팅 대기 로직 (비동기) ---
    private async Awaitable<BattleUnit> WaitForPlayerTargetSelection(BattleUnit attacker)
    {
        Debug.Log($"<color=green>▶ {attacker.unitName}의 턴!</color> 공격할 타겟(적)을 마우스로 클릭하세요...");

        // 비동기 대기를 위한 객체 생성
        var completionSource = new AwaitableCompletionSource<BattleUnit>();

        // 1. 이벤트 구독용 로컬 함수 생성 (클릭 시 호출됨)
        void OnEnemyClicked(BattleUnit selectedUnit)
        {
            // 적군 파티에 속한 놈인지 한 번 더 검증
            if (enemyParty.Contains(selectedUnit))
            {
                // 대기를 풀고 선택된 타겟을 반환 (SetResult)
                completionSource.SetResult(selectedUnit);
            }
        }

        // 2. 이벤트 구독 (클릭 대기 시작)
        TargetClickable.OnTargetClicked += OnEnemyClicked;

        // 3. 여기서 코드 실행이 멈추고 유저가 클릭할 때까지 무한 대기함
        BattleUnit target = await completionSource.Awaitable;

        // 4. 클릭을 받아서 대기가 풀리면 이벤트 구독 해제 (메모리 누수 방지)
        TargetClickable.OnTargetClicked -= OnEnemyClicked;

        return target;
    }

    // --- 실제 리듬 게임 및 데미지 실행 ---
    private async Awaitable ExecutePlayerAttack(BattleUnit attacker, BattleUnit target)
    {
        string chartId = "Player_Attack_01";
        _currentTurnCTS = new CancellationTokenSource();

        try
        {
            // 리듬 게임 진행 (팀원 시스템 호출)
            RhythmResult result = await _rhythmManager.PlayRhythmGameAsync(chartId, true, _currentTurnCTS.Token);

            float damage = 20f * result.totalAccuracy; // 공격자의 스탯을 기반으로 계산하도록 확장 가능
            target.TakeDamage(damage);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[{attacker.unitName}]의 공격이 강제 취소되었습니다.");
        }
        finally
        {
            _currentTurnCTS?.Dispose();
            _currentTurnCTS = null;
        }

        await Awaitable.WaitForSecondsAsync(0.5f);
    }

    private async Awaitable ExecuteEnemyAttack(BattleUnit attacker, BattleUnit target)
    {
        string chartId = "Enemy_Attack_01";
        _currentTurnCTS = new CancellationTokenSource();

        try
        {
            // 플레이어가 방어 리듬 게임 진행
            RhythmResult result = await _rhythmManager.PlayRhythmGameAsync(chartId, false, _currentTurnCTS.Token);

            float baseDamage = 30f;
            float damageTaken = baseDamage * (1f - result.totalAccuracy);

            target.TakeDamage(damageTaken);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[{attacker.unitName}]의 공격(플레이어 방어)이 강제 취소되었습니다.");
        }
        finally
        {
            _currentTurnCTS?.Dispose();
            _currentTurnCTS = null;
        }

        await Awaitable.WaitForSecondsAsync(0.5f);
    }

    // 승패 체크 로직은 playerParty와 enemyParty 리스트를 각각 검사하도록 수정됨
    private bool CheckBattleEndCondition()
    {
        bool isAllPlayersDead = playerParty.All(u => u == null || u.IsDead);
        if (isAllPlayersDead)
        {
            Debug.Log("=== 패배: 아군 전멸 ===");
            return true;
        }

        bool isAllEnemiesDead = enemyParty.All(u => u == null || u.IsDead);
        if (isAllEnemiesDead)
        {
            Debug.Log("=== 승리: 적군 전멸 ===");
            return true;
        }
        return false;
    }
}