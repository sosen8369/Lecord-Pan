using System;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{


    [Header("Sprite Settings")]
    public SpriteRenderer spriteRenderer; 
    public Sprite frontSprite; // 앞모습 이미지
    public Sprite backSprite;  // 뒷모습 이미지 (당장은 비워둡니다)
    
    public string unitName;
    public bool isPlayerTeam; // true면 아군, false면 적군

    public float maxHP = 100f;
    public float currentHP;

    public float attackPower = 80f; 
    public float defensePower = 40f;

    [Header("Attack Settings(고유 공격 패턴)")]
    public AttackPatternData attackPattern;
  
    
    [Header("Chrous Attack Settings")]
    public int chorusCost = 30; // 스킬 사용 시 소모되는 코러스(TP) 양

    [Header("연출(Focus) 설정")]
    public GameObject spotlight; // 캐릭터 자식으로 있는 스포트라이트 오브젝트 연결
    public Vector3 originalPosition{get; private set;}



    // 캐릭터 사망 시 턴 매니저나 UI 시스템에 즉시 알리기 위한 이벤트
    public event Action<BattleUnit> OnDeath;
    private void Awake() 
    {
        currentHP = maxHP;
        originalPosition = transform.position; // 원래 위치 기억
        if (spotlight != null) spotlight.SetActive(false); // 시작할 땐 스포트라이트 끄기
    }

    // 턴 매니저가 이 캐릭터의 턴이 시작/종료될 때 부를 함수
    public void SetFocus(bool isFocused)
    {
        if (isFocused)
        {
            // Z축(또는 Y축)으로 살짝 튀어나오게 설정 (게임 카메라 뷰에 따라 Vector3 값 수정 필요)
            transform.position = originalPosition + new Vector3(2f, 0, 0);
            if (spotlight != null) spotlight.SetActive(true);
        }
        else
        {
            // 원래 자리로 복귀
            transform.position = originalPosition;
            if (spotlight != null) spotlight.SetActive(false);
        }
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

    public void SetBackView(bool isBack)
    {
        if (spriteRenderer == null) return;

        if (isBack)
        {
            // 1. 뒷모습 리소스가 할당되어 있다면 그걸로 교체
            if (backSprite != null) 
            {
                spriteRenderer.sprite = backSprite;
            }
            // 2. 리소스가 없다면 임시로 색상을 어둡게 칠해 뒷모습임을 명시
            else 
            {
                spriteRenderer.color = Color.gray; 
            }
        }
        else
        {
            // 앞모습으로 원상 복구
            if (frontSprite != null) 
            {
                spriteRenderer.sprite = frontSprite;
            }
            spriteRenderer.color = Color.white;
        }
    }

    public virtual void UseSkill()
    {
        Debug.Log($"<color=magenta>[{unitName}]의 고유 스킬 발동!</color>");
        // TODO: 기획에 따라 파티 체력 회복, 방어력 증가 등의 효과를 여기에 작성합니다.
    }

    public bool IsDead => currentHP <= 0;
}