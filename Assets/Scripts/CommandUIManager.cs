using UnityEngine;
using UnityEngine.UI;

// 어떤 행동을 선택했는지 정의
public enum CommandType { Attack, Skill, Cancel }

public class CommandUIManager : MonoBehaviour
{
    public GameObject uiPanel; // 두 버튼을 묶고 있는 부모 패널
    public Button attackButton;
    public Button skillButton;

    [Header("Instruction Panel")]
    public GameObject targetInstructionPanel;
    [Header("Splash Panel")]
    public GameObject enhancedAttackSplashPanel;

    // 카메라 추적을 위한 참조 (캐릭터 옆에 UI를 띄우기 위함)
    private Camera _mainCamera;


    private AwaitableCompletionSource<CommandType> _completionSource;

    private void Awake()
    {
        _mainCamera = Camera.main;

        // 버튼에 이벤트 달기
        attackButton.onClick.AddListener(() => OnButtonClicked(CommandType.Attack));
        skillButton.onClick.AddListener(() => OnButtonClicked(CommandType.Skill));

        uiPanel.SetActive(false); // 평소엔 숨김
        // 시작할 때 강화 공격 패널도 무조건 꺼둡니다.
        if (enhancedAttackSplashPanel != null) enhancedAttackSplashPanel.SetActive(false);
        if (targetInstructionPanel != null) targetInstructionPanel.SetActive(false);
    }

    public void ShowTargetInstruction(bool show)
    {
        if (targetInstructionPanel != null)
        {
            targetInstructionPanel.SetActive(show);
        }
    }

    public async Awaitable PlayEnhancedAttackSplashAsync()
    {
        if (enhancedAttackSplashPanel != null)
        {
            enhancedAttackSplashPanel.SetActive(true);
            
            // 1.5초 동안 패널을 화면에 띄워두고 진행을 멈춤 (여운 주기)
            await Awaitable.WaitForSecondsAsync(1.5f); 
            
            enhancedAttackSplashPanel.SetActive(false);
        }
    }

    // 턴 매니저가 호출할 비동기 대기 함수
    public async Awaitable<CommandType> WaitForCommandAsync(BattleUnit activeUnit)
    {
        // 1. UI를 현재 턴인 캐릭터 옆으로 이동 (Screen Space - Overlay 캔버스 기준)
        // 캐릭터의 3D 월드 좌표를 화면 2D 좌표로 변환하여 UI 패널 위치 설정
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(activeUnit.transform.position);

        // 캐릭터 중심에서 약간 오른쪽 위로 오프셋 이동
        uiPanel.transform.position = screenPos + new Vector3(100f, 50f, 0f);

        // 2. UI 켜기
        uiPanel.SetActive(true);

        // 3. 버튼 클릭 무한 대기
        _completionSource = new AwaitableCompletionSource<CommandType>();
        CommandType result = await _completionSource.Awaitable;

        // 4. 클릭 후 UI 다시 숨기기
        uiPanel.SetActive(false);

        return result;
    }

    private void OnButtonClicked(CommandType type)
    {
        // 대기 중인 상태면 결과를 반환하고 대기를 품
        if (_completionSource != null)
        {
            _completionSource.SetResult(type);
            _completionSource = null;
        }
    }


}