using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RhythmLogicController : MonoBehaviour
{
    // 게임 정상 종료 시 결과를 전달할 이벤트
    public event Action<RhythmResult> OnGameFinished;
    private RhythmGameChart currentChart;
    private int totalNotesCount;

    [Header("Audio")]
    [SerializeField] private AudioSource[] audioSources; // 재생 중인 다중 트랙 오디오

    [Header("Game Settings")]
    [SerializeField] private float noteLeadTimeSec = 2.0f; // 스폰에서 판정선까지의 이동 시간(초)

    [Header("Note Spawning")]
    [SerializeField] private GameObject tapNotePrefab;  // 테스트용 큐브 또는 이미지
    [SerializeField] private Transform[] laneParents;  // 4개의 레인 시작 지점 (Transform 배열)
    [SerializeField] private RectTransform judgmentLine; // 판정선 위치 (Y 좌표 기준점)

    private int nextSpawnIndex = 0; // 다음 생성할 노트의 배열 인덱스
    private List<GameObject> activeNotes = new List<GameObject>(); // 현재 화면에 떠 있는 노트들
    private Queue<Note>[] laneNoteQueues = new Queue<Note>[4];
    
    private bool isPlaying = false; // 메인 루프 가동 여부

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
        // 현재 하드웨어 시간을 가져옵니다.
        double currentTime = AudioSettings.dspTime;
        
        // 오디오가 실제로 재생될 미래의 시간을 계산합니다.
        double scheduledTime = currentTime + noteLeadTimeSec;

        // 시간 계산용 기준 변수들을 초기화합니다.
        dspStartTime = scheduledTime;
        accumulatedPauseTime = 0;
        isPaused = false;

        // 배열에 등록된 모든 오디오 트랙을 예약 재생합니다.
        foreach (var source in audioSources)
        {
            if (source.clip != null)
            {
                source.PlayScheduled(scheduledTime);
            }
        }

        // Update 루프가 동작하도록 플래그를 활성화합니다.
        isPlaying = true;
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

    private void Update()
    {
        if (!isPlaying || isPaused) return;

        float currentMs = CurrentMusicTimeMs;

        // 1. 노트 스폰 검사 (Spawn Check)
        // 현재 시간 + 리드 타임이 노트의 목표 시간보다 크면 스폰합니다.
        while (nextSpawnIndex < currentChart.notes.Length && 
               currentMs + (noteLeadTimeSec * 1000f) >= currentChart.notes[nextSpawnIndex].timeMs)
        {
            SpawnNote(currentChart.notes[nextSpawnIndex]);
            nextSpawnIndex++;
        }

        // 2. 노트 위치 갱신 (Movement Update)
        UpdateNotePositions(currentMs);
    }

    private void SpawnNote(NoteData note)
    {
        // 원래는 오브젝트 풀러를 사용해야 하지만, 지금은 테스트를 위해 Instantiate를 사용합니다.
        GameObject noteObj = Instantiate(tapNotePrefab, laneParents[note.lane]);
        
        // 노트 객체에 자신의 데이터를 저장하거나 초기화하는 로직 (나중에 확장)
        activeNotes.Add(noteObj);
        
        // 테스트용: 노트 객체의 이름을 타겟 시간으로 설정하여 구분
        noteObj.name = $"Note_{note.timeMs}";
    }

    private void UpdateNotePositions(float currentMs)
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            GameObject noteObj = activeNotes[i];
            // 예시로 이름에 저장된 타겟 시간을 파싱하여 사용 (실제로는 클래스로 관리)
            float targetTimeMs = float.Parse(noteObj.name.Split('_')[1]);
            
            // 판정선과의 시간 차이 계산
            float timeDiff = targetTimeMs - currentMs;

            // 시간 차에 따른 Y축 위치 계산 (단순 선형 이동)
            // 배속 이벤트(SpeedEvent)는 나중에 이 공식에 multiplier를 곱해 적용합니다.
            float yPos = (timeDiff / 1000f) * (judgmentLine.anchoredPosition.y / noteLeadTimeSec);
            
            // 판정선 위치를 기준으로 오프셋 이동
            RectTransform rt = noteObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, yPos + judgmentLine.anchoredPosition.y);

            // 판정선을 지나쳐서 화면 밖으로 완전히 나간 경우 (예: -200ms) 일단 삭제
            if (timeDiff < -200f)
            {
                activeNotes.RemoveAt(i);
                Destroy(noteObj);
            }
        }
    }
}