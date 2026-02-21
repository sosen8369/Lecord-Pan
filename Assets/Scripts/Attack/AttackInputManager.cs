using UnityEngine;
using UnityEngine.InputSystem;

public class AttackInputManager : MonoBehaviour
{
    private InputSystem_Actions controls; 
    private AttackRhythmController attackController;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        attackController = GetComponent<AttackRhythmController>();

        // 수정된 부분: AttackPlayer 액션 맵의 AttackAction을 구독합니다.
        // 이 액션은 에디터에서 Space 키로 할당되어 있어야 합니다.
        controls.AttackPlayer.AttackAction.performed += _ => attackController.OnInput();
    }

    private void OnEnable()
    {
        // 수정된 부분: AttackPlayer 액션 맵을 활성화합니다.
        controls.AttackPlayer.Enable();
    }

    private void OnDisable()
    {
        // 수정된 부분: AttackPlayer 액션 맵을 비활성화합니다.
        controls.AttackPlayer.Disable();
    }
}