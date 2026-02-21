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
    [SerializeField] private AttackGameManager attackGameManager;
    [SerializeField] private CommandUIManager _commandUIManager;


    [Header("Party Shared HP (밴드 공유 체력)")]
    public float partyMaxHP;
    public float partyCurrentHP {get; private set;}

    [Header("Party Chorus (밴드 코러스)")]
    public int partyMaxChorus = 100;
    public int partyCurrentChorus = 0;

    [Header("Party Management")]
    public List<BattleUnit> playerParty = new List<BattleUnit>();
    public List<BattleUnit> enemyParty = new List<BattleUnit>();

    [Header("Spawn Settings")]
    public List<Transform> playerSpawnPositions; // ★ 전투 시작 시 아군 4명이 소환될 위치 (빈 오브젝트 4개)


    [Header("Camera Settings")]
    public Camera mainCamera; // 씬의 메인 카메라
    public Transform defenseCameraView; // 4패드 방어 시 바라볼 카메라의 위치/각도 (빈 오브젝트로 씬에 배치)

    // ★ 방어 할때만 위치를 일직선으로 바꾸어야 함
    public List<Transform> defensePositions;

    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;
    // 팀원 명세서에 맞춘 강제 종료 신호 토큰 관리 객체
    private CancellationTokenSource turnCts;

    private async void Start()
    {
        await StartBattleLoop();
    }

    private void Awake()
    {{
        if (mainCamera != null)
        {
            _originalCamPos = mainCamera.transform.position;
            _originalCamRot = mainCamera.transform.rotation;
        }

        // 1. 전투 씬이 켜지자마자 GameManager에서 멤버를 받아와 소환합니다.
        SpawnPlayerParty(); 

        // 2. ★ 소환된 캐릭터들의 개별 체력을 합산하여 파티의 총 체력을 동적으로 결정합니다.
        partyMaxHP = 0f;
        foreach (var player in playerParty)
        {
            if (player != null) 
            {
                partyMaxHP += player.maxHP;
            }
        }

        // [방어 코드] 만약 캐릭터가 하나도 없거나 체력이 0으로 설정되어 있다면 즉사를 막기 위해 기본값 부여
        if (partyMaxHP <= 0f) partyMaxHP = 100f;

        partyCurrentHP = partyMaxHP; // 합산된 최대 체력으로 시작 체력 충전
        Debug.Log($"<color=green>[시스템] 출전 인원: {playerParty.Count}명 / 파티 총 최대 체력: {partyMaxHP}</color>");
    }
    }

    private void SpawnPlayerParty()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("씬에 GameManager가 없습니다!");
            return;
        }

        var partyPrefabs = GameManager.Instance.selectedPartyPrefabs;
        playerParty.Clear();

        for (int i = 0; i < partyPrefabs.Count; i++)
        {
            if (i >= playerSpawnPositions.Count) 
            {
                Debug.LogWarning("소환 위치보다 선택된 캐릭터가 많습니다.");
                break;
            }

            GameObject prefab = partyPrefabs[i];
            Transform spawnPoint = playerSpawnPositions[i];

            // 지정된 위치에 캐릭터 프리팹 생성
            GameObject spawnedChar = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            BattleUnit unit = spawnedChar.GetComponent<BattleUnit>();

            if (unit != null)
            {
                playerParty.Add(unit);
            }
        }
        Debug.Log($"<color=cyan>[시스템] GameManager로부터 {playerParty.Count}명의 아군을 성공적으로 소환했습니다.</color>");
    }

    // --- 카메라와 캐릭터 진형을 동시에 부드럽게 이동 ---
    private async Awaitable MoveCameraAndFormationAsync(Transform targetCamView, bool isReturning, float duration)
    {
        if (mainCamera == null) return;

        Vector3 startCamPos = mainCamera.transform.position;
        Quaternion startCamRot = mainCamera.transform.rotation;
        
        Vector3 targetCamPos = targetCamView != null ? targetCamView.position : _originalCamPos;
        Quaternion targetCamRot = targetCamView != null ? targetCamView.rotation : _originalCamRot;

        // 이동 시작 전 캐릭터들의 출발 위치 기억
        Vector3[] startPlayerPos = new Vector3[playerParty.Count];
        for (int i = 0; i < playerParty.Count; i++)
        {
            if (playerParty[i] != null) startPlayerPos[i] = playerParty[i].transform.position;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // SmoothStep (자연스러운 감속)

            // 1. 카메라 부드럽게 이동
            mainCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startCamRot, targetCamRot, t);

            // 2. 캐릭터 부드럽게 진형(일직선/제자리)으로 이동
            for (int i = 0; i < playerParty.Count; i++)
            {
                if (playerParty[i] != null && !playerParty[i].IsDead)
                {
                    Vector3 targetPos = playerParty[i].originalPosition; // 복귀할 때는 제자리로
                    
                    // 방어하러 갈 때 && 지정된 방어 위치(Slot)가 비어있지 않을 때
                    if (!isReturning && defensePositions != null && i < defensePositions.Count && defensePositions[i] != null)
                    {
                        targetPos = defensePositions[i].position;
                    }

                    playerParty[i].transform.position = Vector3.Lerp(startPlayerPos[i], targetPos, t);
                }
            }
            
            await Awaitable.NextFrameAsync();
        }

        // 오차 보정을 위해 최종 위치로 확실하게 고정 (Snap)
        mainCamera.transform.position = targetCamPos;
        mainCamera.transform.rotation = targetCamRot;
        
        for (int i = 0; i < playerParty.Count; i++)
        {
            if (playerParty[i] != null && !playerParty[i].IsDead)
            {
                Vector3 finalPos = playerParty[i].originalPosition;
                if (!isReturning && defensePositions != null && i < defensePositions.Count && defensePositions[i] != null)
                {
                    finalPos = defensePositions[i].position;
                }
                playerParty[i].transform.position = finalPos;
            }
        }
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

            currentAttacker.SetFocus(true);

            try
            {
                CommandType selectedCommand = await _commandUIManager.WaitForCommandAsync(currentAttacker);
                
                // 1. 코러스(TP) 검사: 스킬(강화공격)을 눌렀는지 확인
                bool isEnhanced = (selectedCommand == CommandType.Skill);
                
                // 코러스가 부족한데 강화 공격을 눌렀다면? (턴을 날리지 않고 일반 공격으로 자동 강등)
                if (isEnhanced && partyCurrentChorus < currentAttacker.chorusCost)
                {
                    Debug.LogWarning($"<color=red>코러스(TP)가 부족합니다! (현재:{partyCurrentChorus} / 필요:{currentAttacker.chorusCost}) 일반 공격으로 대체됩니다.</color>");
                    isEnhanced = false; 
                }

                // 2. 공통 타겟팅 (무조건 적을 클릭)
                BattleUnit target = await WaitForPlayerTargetSelection(currentAttacker);
                if (target == null) continue;

                // 3. 강화 공격일 경우 코러스 즉시 차감
                if (isEnhanced)
                {
                    partyCurrentChorus -= currentAttacker.chorusCost;
                    Debug.Log($"코러스 {currentAttacker.chorusCost} 소모. 남은 코러스: {partyCurrentChorus}");
                }

                // 4. 단일화된 공격 실행 로직 호출
                await ExecuteAttackTurn(currentAttacker, target, isEnhanced);
            }
            finally
            {
                currentAttacker.SetFocus(false);
            }

            if (CheckBattleEndCondition()) return;
        }
    }

private async Awaitable ExecuteAttackTurn(BattleUnit attacker, BattleUnit target, bool isEnhanced)
    {
        if (attacker.attackPattern == null)
        {
            Debug.LogError($"[{attacker.unitName}]의 패턴이 없습니다!");
            return;
        }

        try
        {
            // 코러스를 소모한 강화 공격이면 기본 공격력을 1.5배 뻥튀기
            float attackMultiplier = isEnhanced ? 1.5f : 1.0f;
            string attackType = isEnhanced ? "<color=magenta>강화 공격(1.5배)</color>" : "일반 공격";
            
            Debug.Log($"[시스템] {attacker.unitName}의 {attackType} 시작!");

            // 1. 1패드 리듬 게임 실행
            RhythmResult result = await attackGameManager.PlayAttackAsync(attacker.attackPattern);

            // 2. 기획자 공식 적용: 아군 공격력 = 공격력 * (100/(100+적방어력)) * (0.6 + 0.9 * 리듬계수)
            float finalAttackPower = attacker.attackPower * attackMultiplier;
            float defenseFactor = 100f / (100f + target.defensePower);
            float rhythmFactor = 0.6f + (0.9f * result.totalAccuracy);

            float finalDamage = finalAttackPower * defenseFactor * rhythmFactor;
            
            // 데미지 텍스트 로그 상세 출력
            Debug.Log($"<color=cyan>[데미지 연산] 타격:{finalAttackPower} * 방어계수:{defenseFactor:F2} * 리듬계수:{rhythmFactor:F2} = 최종 {finalDamage:F1} 데미지!</color>");
            target.TakeDamage(finalDamage);

            // 3. 코러스(TP) 적립 로직 (일반 공격일 때만)
            if (!isEnhanced) 
            {
                int earnedChorus = Mathf.RoundToInt(result.totalAccuracy * 15f); 
                partyCurrentChorus += earnedChorus;
                if (partyCurrentChorus > partyMaxChorus) partyCurrentChorus = partyMaxChorus; 
                Debug.Log($"<color=yellow>[코러스 획득] +{earnedChorus} / 현재 코러스: {partyCurrentChorus}</color>");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"공격 취소 또는 에러: {e.Message}");
        }
        
        await Awaitable.WaitForSecondsAsync(0.5f);
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

    // --- 적군 페이즈 ---
    private async Awaitable EnemyPhase()
    {
        Debug.Log("\n[적군 페이즈 시작]");
        foreach (var currentEnemy in enemyParty)
        {
            if (currentEnemy == null || currentEnemy.IsDead) continue;

            // 아군은 개별로 죽지 않으므로, 그냥 첫 번째 자리에 있는 캐릭터를 타겟으로 잡고 방어(4패드)를 시작합니다.
            BattleUnit targetPlayer = playerParty.FirstOrDefault(u => u != null);
            if (targetPlayer != null)
            {
                await ExecuteEnemyTurn(currentEnemy, targetPlayer);
            }

            if (CheckBattleEndCondition()) return;
        }
    }

    // --- 적군 방어 리듬 게임 로직 (공유 체력 적용) ---
    private async Awaitable ExecuteEnemyTurn(BattleUnit attacker, BattleUnit target)
    {
        string chartId = "Test Battle Track"; 
        turnCts = new CancellationTokenSource();

        try
        {
            Debug.Log("<color=red>[시스템] 적 공격! 방어 시점으로 이동!</color>");
            
            foreach (var player in playerParty)
            {
                if (player != null) player.SetBackView(true); 
            }

            await MoveCameraAndFormationAsync(defenseCameraView, false, 0.5f);

            RhythmResult result = await rhythmManager.PlayRhythmGameAsync(chartId, false, turnCts.Token);
            
            // ★ 기획자 공식 적용: 적 공격력 = 공격력 * (100/(100+아군방어력)) * (1.4 - 0.8 * 리듬계수)
            float baseAttack = attacker.attackPower;
            float defenseFactor = 100f / (100f + target.defensePower); // 타겟팅된 아군의 개별 방어력 적용
            float rhythmFactor = 1.4f - (0.8f * result.totalAccuracy);

            float damageTaken = baseAttack * defenseFactor * rhythmFactor;

            // 최종 데미지는 파티 공유 체력에서 차감
            partyCurrentHP -= damageTaken;
            Debug.Log($"<color=orange>[방어 연산] 적 타격:{baseAttack} * 아군 방어계수:{defenseFactor:F2} * 리듬계수:{rhythmFactor:F2} = 최종 {damageTaken:F1} 피해!</color>");
            Debug.Log($"<color=orange>남은 파티 체력: {partyCurrentHP:F1} / {partyMaxHP}</color>");
            
        }
        catch (OperationCanceledException)
        {
            Debug.Log("적 턴(방어) 리듬 게임이 강제 취소되었습니다.");
        }
        finally
        {
            turnCts?.Dispose();
            turnCts = null;

            Debug.Log("<color=cyan>[시스템] 원래 시점과 원래 진형으로 복귀</color>");
            await MoveCameraAndFormationAsync(null, true, 0.5f);

            foreach (var player in playerParty)
            {
                if (player != null) player.SetBackView(false);
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


    // --- 승패 조건 체크 (공유 체력 기반으로 변경) ---
    private bool CheckBattleEndCondition()
    {
        // 아군은 개별 IsDead가 아니라 공유 체력이 0 이하인지로 패배를 판정합니다.
        if (partyCurrentHP <= 0)
        {
            partyCurrentHP = 0;
            Debug.Log("=== 패배: 밴드의 연주력(체력)이 모두 소진되었습니다! (마에스트로 쓰러짐) ===");
            return true;
        }
        
        // 적군은 여전히 개별 체력(IsDead)을 기준으로 전멸 여부를 판정합니다.
        if (enemyParty.All(u => u == null || u.IsDead))
        {
            Debug.Log("=== 승리: 적군 전멸 ===");
            return true;
        }
        return false;
    }
}