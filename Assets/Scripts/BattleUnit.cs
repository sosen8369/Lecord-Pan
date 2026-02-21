using System;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public string unitName;
    public bool isPlayerTeam; // true면 아군, false면 적군
    
    public float maxHP = 100f;
    public float currentHP;

    // 캐릭터 사망 시 턴 매니저나 UI 시스템에 즉시 알리기 위한 이벤트
    public event Action<BattleUnit> OnDeath;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        // 이미 죽은 상태라면 데미지 무시 (오버킬 및 중복 사망 이벤트 방지)
        if (IsDead) return;

        currentHP -= damage;
        Debug.Log($"[{unitName}] 피격! 데미지: {damage} / 남은 HP: {currentHP}");
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            Debug.Log($"[{unitName}] 사망했습니다.");
            
            // 나 죽었다고 구독자(UI, 애니메이터, 턴 매니저 등)에게 방송
            OnDeath?.Invoke(this); 
        }
    }

    public bool IsDead => currentHP <= 0;
}