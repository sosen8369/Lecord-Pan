using UnityEngine;
using System.Threading.Tasks;
using System;

public class AttackGameManager : MonoBehaviour
{[Header("Dependencies")]
    [SerializeField] private AttackRhythmController attackController;
    
    [Header("UI Settings")]
    public GameObject attackPanel; // ★ 1패드 리듬 게임 캔버스(또는 패널) 연결용

    private TaskCompletionSource<RhythmResult> completionSource;

    private void Awake()
    {
        // 1. 게임 시작 시, 클릭 방해를 막기 위해 1패드 패널을 강제로 꺼둡니다.
        if (attackPanel != null) 
        {
            attackPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 턴 매니저가 공격을 시작할 때 호출하는 함수입니다.
    /// </summary>
    public Task<RhythmResult> PlayAttackAsync(AttackPatternData patternData)
    {
        // 2. ★ 리듬 게임 시작: 화면에 패널을 띄웁니다.
        if (attackPanel != null) attackPanel.SetActive(true);

        completionSource = new TaskCompletionSource<RhythmResult>();
        attackController.OnAttackFinished += HandleAttackFinished;
        attackController.StartAttack(patternData);

        return completionSource.Task;
    }

    private void HandleAttackFinished(RhythmResult result)
    {
        attackController.OnAttackFinished -= HandleAttackFinished;

        // 3. ★ 리듬 게임 종료: 다음 턴의 버튼 클릭을 위해 패널을 즉시 숨깁니다.
        if (attackPanel != null) attackPanel.SetActive(false);

        completionSource.SetResult(result);
    }
}