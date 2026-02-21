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
    [Header("Managers")]
    [SerializeField] private JudgmentManager judgmentManager; // 에러 해결을 위한 선언
    [SerializeField] private ObjectPoolManager poolManager;   // 에러 해결을 위한 선언

    [Header("Audio")]
    [SerializeField] private AudioSource[] audioSources; // 재생 중인 다중 트랙 오디오

    [Header("Game Settings")]
    [SerializeField] private float noteLeadTimeSec = 2.0f; // 스폰에서 판정선까지의 이동 시간(초)

    [Header("Note Spawning")]
    [SerializeField] private GameObject tapNotePrefab;  // 테스트용 큐브 또는 이미지
    [SerializeField] private Transform[] laneParents;  // 4개의 레인 시작 지점 (Transform 배열)
    [SerializeField] private RectTransform judgmentLine; // 판정선 위치 (Y 좌표 기준점)

    private int nextSpawnIndex = 0; // 다음 생성할 노트의 배열 인덱스
    private List<Note> activeNotes = new List<Note>();
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
        TextAsset jsonTextAsset = Resources.Load<TextAsset>($"Charts/{chartID}");
        if (jsonTextAsset == null)
        {
            Debug.LogError($"채보 파일을 찾을 수 없습니다: Charts/{chartID}");
            return;
        }

        currentChart = JsonUtility.FromJson<RhythmGameChart>(jsonTextAsset.text);
        audioOffsetMs = currentChart.audioOffsetMs;

        if (currentChart.notes != null)
        {
            totalNotesCount = currentChart.notes.Length;
        }
        else
        {
            totalNotesCount = 0;
        }

        // 추가된 부분: 판정 매니저 내부 데이터 초기화
        judgmentManager.Initialize();
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

    private void Awake()
    {
        // 4개의 레인 큐 초기화
        for (int i = 0; i < 4; i++)
        {
            laneNoteQueues[i] = new Queue<Note>();
        }
    }

    public void OnInput(int lane)
    {
        // 게임 중이 아니거나 일시정지 상태면 입력을 무시합니다.
        if (!isPlaying || isPaused) return;

        // 해당 레인의 큐에 판정할 노트가 있는지 확인합니다.
        if (laneNoteQueues[lane].Count > 0)
        {
            // JudgmentManager에게 판정 로직을 위임합니다.
            // 현재 재생 시간(CurrentMusicTimeMs)과 해당 레인의 큐를 전달합니다.
            judgmentManager.ProcessJudgment(lane, CurrentMusicTimeMs, laneNoteQueues[lane]);
        }
    }

    private void CheckGameEnd()
    {
        // 채보의 모든 노트가 스폰되었고, 화면에 남은 노트가 없으며, 메인 오디오 재생이 끝났을 때
        if (nextSpawnIndex >= currentChart.notes.Length && 
            activeNotes.Count == 0 && 
            audioSources.Length > 0 && 
            !audioSources[0].isPlaying)
        {
            isPlaying = false;
            
            // 턴 매니저에게 전달할 최종 결과 산출 및 이벤트 호출
            RhythmResult finalResult = judgmentManager.GetFinalResult(totalNotesCount);
            OnGameFinished?.Invoke(finalResult);
        }
    }

    private void Update()
    {
        if (!isPlaying || isPaused) return;

        float currentMs = CurrentMusicTimeMs;

        while (nextSpawnIndex < currentChart.notes.Length && 
               currentMs + (noteLeadTimeSec * 1000f) >= currentChart.notes[nextSpawnIndex].timeMs)
        {
            SpawnNote(currentChart.notes[nextSpawnIndex]);
            nextSpawnIndex++;
        }

        UpdateNotePositions(currentMs);
        CheckMissNotes(currentMs);
        
        // 추가된 부분: 매 프레임 게임 종료 조건 검사
        CheckGameEnd();
    }

    private void CheckMissNotes(float currentMs)
    {
        for (int i = 0; i < 4; i++)
        {
            if (laneNoteQueues[i].Count > 0)
            {
                Note frontNote = laneNoteQueues[i].Peek();
                
                if (currentMs - frontNote.Data.timeMs > judgmentManager.MissThresholdMs) 
                {
                    judgmentManager.ProcessMiss(laneNoteQueues[i]);
                    // 기존에 있던 activeNotes.Remove(frontNote.gameObject); 구문을 삭제합니다.
                }
            }
        }
    }

    private void SpawnNote(NoteData note)
    {
        GameObject noteObj = poolManager.GetFromPool(note.type, laneParents[note.lane]);
        Note noteComponent = noteObj.GetComponent<Note>();
        
        noteComponent.Setup(note);
        laneNoteQueues[note.lane].Enqueue(noteComponent);
        
        // 수정된 부분: GameObject 대신 Note 컴포넌트를 리스트에 추가합니다.
        activeNotes.Add(noteComponent);
    }

    private void UpdateNotePositions(float currentMs)
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            Note note = activeNotes[i];
            
            // 1. 리스트 동기화 및 풀 반환
            // JudgmentManager에 의해 판정 처리되어 비활성화된 노트를 일괄 정리합니다.
            if (!note.gameObject.activeSelf)
            {
                activeNotes.RemoveAt(i);
                poolManager.ReturnToPool(note.Data.type, note.gameObject);
                continue;
            }
            
            // 2. 문자열 파싱(Split) 제거 및 정규화된 데이터 직접 참조
            float timeDiff = note.Data.timeMs - currentMs;
            
            // Note 컴포넌트에 캐싱된 RectTransform(Rect)을 바로 사용합니다.
            float yPos = (timeDiff / 1000f) * (judgmentLine.anchoredPosition.y / noteLeadTimeSec);
            note.Rect.anchoredPosition = new Vector2(0, yPos + judgmentLine.anchoredPosition.y);

            // 3. 안전장치: 판정 처리 없이 화면을 아득히 벗어난 오류 노트 정리
            if (timeDiff < -judgmentManager.MissThresholdMs - 200f)
            {
                activeNotes.RemoveAt(i);
                
                // 큐에 남아있다면 함께 정리하여 진행 불가(Block) 현상을 방지합니다.
                if (laneNoteQueues[note.Data.lane].Count > 0 && laneNoteQueues[note.Data.lane].Peek() == note)
                {
                    laneNoteQueues[note.Data.lane].Dequeue();
                }
                
                // Destroy를 피하고 풀러에 반환합니다.
                poolManager.ReturnToPool(note.Data.type, note.gameObject);
            }
        }
    }
}