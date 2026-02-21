using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmLogicController : MonoBehaviour
{
    // 게임 정상 종료 시 결과를 전달할 이벤트
    public event Action<RhythmResult> OnGameFinished;
    private RhythmGameChart currentChart;
    private int totalNotesCount;

    [Header("Audio")]
    [SerializeField] private AudioSource[] audioSources; // 재생 중인 다중 트랙 오디오

    // 시간 계산용 내부 변수
    private double dspStartTime;         // 게임이 시작된 정확한 하드웨어 시간
    private double pausedDspTime;        // 일시정지 버튼을 누른 시점의 시간
    private double accumulatedPauseTime; // 누적된 총 일시정지 시간
    private float audioOffsetMs;         // JSON 채보에서 로드할 시작 여백 시간
    private bool isPaused;

    /// <summary>
    /// 현재 재생 중인 음악의 실제 진행 시간(밀리초)을 반환합니다.
    /// 스포너와 판정 매니저가 이 값을 참조하여 노트를 처리합니다.
    /// </summary>
    public float CurrentMusicTimeMs
    {
        get
        {
            if (isPaused)
            {
                // 일시정지 중에는 시간이 흐르지 않도록 멈춘 시점 기준의 시간을 반환합니다.
                return (float)((pausedDspTime - dspStartTime - accumulatedPauseTime) * 1000.0) - audioOffsetMs;
            }
            else
            {
                // 현재 시간에서 시작 시간과 일시정지된 총 시간을 빼서 순수 재생 시간을 도출합니다.
                return (float)((AudioSettings.dspTime - dspStartTime - accumulatedPauseTime) * 1000.0) - audioOffsetMs;
            }
        }
    }

    public void PauseSystem()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f; // 게임 로직 정지
        
        // 일시정지된 시점의 dspTime 기록
        pausedDspTime = AudioSettings.dspTime;

        // 모든 오디오 트랙 일시정지
        foreach (var source in audioSources)
        {
            source.Pause();
        }
    }

    public void ResumeSystem()
    {
        if (!isPaused) return;

        // 일시정지 상태로 머물러 있던 물리적인 시간(초)을 계산하여 누적
        double timeSpentPaused = AudioSettings.dspTime - pausedDspTime;
        accumulatedPauseTime += timeSpentPaused;

        isPaused = false;
        Time.timeScale = 1f; // 게임 로직 재개

        // 모든 오디오 트랙 재생 재개
        foreach (var source in audioSources)
        {
            source.UnPause();
        }
    }

    public void InitializeGameData(string chartID, bool isAttackTurn)
    {
        // 1. Resources 폴더 내의 지정된 경로에서 JSON 텍스트 에셋을 로드합니다.
        // 실제 프로젝트 구조에 따라 Addressables나 File.ReadAllText 방식으로 변경할 수 있습니다.
        TextAsset jsonTextAsset = Resources.Load<TextAsset>($"Charts/{chartID}");
        
        if (jsonTextAsset == null)
        {
            Debug.LogError($"채보 파일을 찾을 수 없습니다: Charts/{chartID}");
            return;
        }

        // 2. JSON 문자열을 RhythmGameChart 객체로 변환합니다.
        currentChart = JsonUtility.FromJson<RhythmGameChart>(jsonTextAsset.text);

        // 3. 곡 시작 여백 시간을 변수에 할당합니다.
        audioOffsetMs = currentChart.audioOffsetMs;

        // 4. 최종 정확도(RhythmResult.totalAccuracy) 계산을 위해 전체 노트 개수를 캐싱합니다.
        if (currentChart.notes != null)
        {
            totalNotesCount = currentChart.notes.Length;
        }
        else
        {
            totalNotesCount = 0;
        }

        // isAttackTurn(공격/방어 상태)에 따라 UI 색상 변경이나 배율 조정 로직이 필요하다면
        // 이 위치에 작성하여 변수에 저장합니다.
    }

    public void StartRhythmLoop()
    {
        // 오디오 스케줄링 및 업데이트 루프 가동 로직 대기 위치
    }

    public void StopAllAudio()
    {
        // 강제 종료 시 오디오 정지 로직 대기 위치
    }

    public void ReturnAllObjectsToPool()
    {
        // 강제 종료 시 오브젝트 풀 반환 로직 대기 위치
    }

    public void ResetSessionData()
    {
        // 강제 종료 시 누적 점수 및 데이터 초기화 로직 대기 위치
    }
}