using System;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System 네임스페이스 추가

[RequireComponent(typeof(Collider))]
public class TargetClickable : MonoBehaviour
{
    private BattleUnit _myUnit;
    public static event Action<BattleUnit> OnTargetClicked;

    private void Awake()
    {
        _myUnit = GetComponent<BattleUnit>();
    }

    private void Update()
    {
        // 1. 마우스 왼쪽 버튼이 '이번 프레임'에 눌렸는지 확인 (New Input System 기준)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckClick();
        }
        
        // 만약 위 코드에서 에러가 난다면(프로젝트 설정 차이), 위 if문을 지우고 아래 코드로 대체하세요.
        // if (Input.GetMouseButtonDown(0)) { CheckClick(); }
    }

    private void CheckClick()
    {
        // 2. 메인 카메라에서 현재 마우스 포인터 위치로 3D 레이저(Ray) 생성
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        // 3. 레이저를 쏴서 뭔가 맞았는지 검사
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 4. 맞은 놈이 '나(이 스크립트가 붙은 오브젝트)'인지 확인
            if (hit.collider.gameObject == gameObject)
            {
                if (_myUnit != null && !_myUnit.IsDead)
                {
                    Debug.Log($"<color=cyan>[타겟 빔 적중!]</color> {_myUnit.unitName} 선택됨!");
                    OnTargetClicked?.Invoke(_myUnit);
                }
            }
        }
    }
}