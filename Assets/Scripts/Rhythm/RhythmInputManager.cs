using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmInputManager : MonoBehaviour
{
    private InputSystem_Actions controls; // 자동 생성된 클래스 활용
    private RhythmLogicController logicController;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        logicController = GetComponent<RhythmLogicController>();

        // 기존 에셋의 RhythmPlayer 맵 내 액션들에 함수를 연결(구독)합니다.
        controls.RhythmPlayer.Lane0.performed += _ => logicController.OnInput(0);
        controls.RhythmPlayer.Lane1.performed += _ => logicController.OnInput(1);
        controls.RhythmPlayer.Lane2.performed += _ => logicController.OnInput(2);
        controls.RhythmPlayer.Lane3.performed += _ => logicController.OnInput(3);
    }

    private void OnEnable()
    {
        controls.RhythmPlayer.Enable(); // 맵 활성화
    }

    private void OnDisable()
    {
        controls.RhythmPlayer.Disable(); // 맵 비활성화
    }
}