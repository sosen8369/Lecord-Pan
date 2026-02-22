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
        // 풀 매니저가 할당되어 있는지 먼저 확인
        if (poolManager == null)
        {
            Debug.LogError("Pool Manager가 할당되지 않았습니다!");
            return;
        }

        // 게임 시작 전 풀을 초기화하여 딕셔너리와 초기 객체들을 생성합니다.
        poolManager.Initialize();
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
        // 모든 오디오 소스 재생 강제 정지 및 초기화
        foreach (var source in audioSources)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null; // 남아있는 오디오 클립 레퍼런스 해제
            }
        }
    }

    public void ReturnAllObjectsToPool()
    {
        // 화면에 남아있는(활성화된) 모든 노트를 강제로 풀(Pool)로 회수
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            Note note = activeNotes[i];
            if (note != null && note.gameObject.activeSelf)
            {
                poolManager.ReturnToPool(note.Data.type, note.gameObject);
            }
        }
        activeNotes.Clear();

        // 4개의 레인 큐(Queue) 내부 데이터 강제 삭제
        for (int i = 0; i < 4; i++)
        {
            if (laneNoteQueues[i] != null)
            {
                laneNoteQueues[i].Clear();
            }
        }
    }

    public void ResetSessionData()
    {
        // ★ 핵심 버그 원인: 다음 생성할 노트 인덱스를 0으로 되돌리지 않으면 다음 턴에 노트가 아예 나오지 않습니다.
        nextSpawnIndex = 0;
        
        isPlaying = false;
        isPaused = false;
        accumulatedPauseTime = 0;
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
        
        // 추가된 부분: 풀러에서 꺼낸 객체의 스케일을 원래 사이즈로 초기화합니다.
        noteComponent.Rect.localScale = Vector3.one;
        
        activeNotes.Add(noteComponent);
    }

    private void UpdateNotePositions(float currentMs)
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            Note note = activeNotes[i];
            
            if (!note.gameObject.activeSelf)
            {
                activeNotes.RemoveAt(i);
                poolManager.ReturnToPool(note.Data.type, note.gameObject);
                continue;
            }
            
            float timeDiff = note.Data.timeMs - currentMs;
            
            // 수정된 이동 공식: 시작점(Lane)과 도착점(Judgment Line) 사이의 절대 위치 계산
            float t = timeDiff / (noteLeadTimeSec * 1000f);
            Vector3 judgePos = judgmentLine.position;
            Vector3 spawnPos = laneParents[note.Data.lane].position;
            
            // LerpUnclamped를 사용하여 노트가 판정선을 지나쳐도(t < 0) 계속 이동하도록 처리합니다.
            Vector3 targetPos = Vector3.LerpUnclamped(judgePos, spawnPos, t);
            
            // X축은 레인의 위치로 고정하고, Y축은 계산된 하강 위치를 적용합니다.
            note.Rect.position = new Vector3(spawnPos.x, targetPos.y, spawnPos.z);

            if (timeDiff < -judgmentManager.MissThresholdMs - 200f)
            {
                activeNotes.RemoveAt(i);
                
                if (laneNoteQueues[note.Data.lane].Count > 0 && laneNoteQueues[note.Data.lane].Peek() == note)
                {
                    laneNoteQueues[note.Data.lane].Dequeue();
                }
                
                poolManager.ReturnToPool(note.Data.type, note.gameObject);
            }
        }
    }



    //Debug
/*
    private void Start() {
        InitializeGameData("Test Battle Track", true);
        StartRhythmLoop();
    }
    */
}