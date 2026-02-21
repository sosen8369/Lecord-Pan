using System;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public string unitName;
    public bool isPlayerTeam; // true면 아군, false면 적군

    public float maxHP = 100f;
    public float currentHP;

    [Header("연출(Focus) 설정")]
    public GameObject spotlight; // 캐릭터 자식으로 있는 스포트라이트 오브젝트 연결
    private Vector3 _originalPosition;

    // 캐릭터 사망 시 턴 매니저나 UI 시스템에 즉시 알리기 위한 이벤트
    public event Action<BattleUnit> OnDeath;
    private void Start() // Awake 말고 Start 사용 권장
    {
        currentHP = maxHP;
        _originalPosition = transform.position; // 원래 위치 기억
        if (spotlight != null) spotlight.SetActive(false); // 시작할 땐 스포트라이트 끄기
    }

    // 턴 매니저가 이 캐릭터의 턴이 시작/종료될 때 부를 함수
    public void SetFocus(bool isFocused)
    {
        if (isFocused)
        {
            // Z축(또는 Y축)으로 살짝 튀어나오게 설정 (게임 카메라 뷰에 따라 Vector3 값 수정 필요)
            transform.position = _originalPosition + new Vector3(2f, 0, 0);
            if (spotlight != null) spotlight.SetActive(true);
        }
        else
        {
            // 원래 자리로 복귀
            transform.position = _originalPosition;
            if (spotlight != null) spotlight.SetActive(false);
        }
    }
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