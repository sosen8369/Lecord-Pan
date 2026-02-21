using UnityEngine;
using System.Collections.Generic;
using System;

public class JudgmentManager : MonoBehaviour
{
    [Header("Judgment Settings")]
    // 등급이 높은 순서(범위가 좁은 순서)대로 인스펙터에서 배치하십시오.
    [SerializeField] private JudgeInfo[] judgeSettings;
    [SerializeField] private JudgeInfo missJudgeInfo = new JudgeInfo { name = "Miss", thresholdMs = 200f, accuracyWeight = 0f };
    public float MissThresholdMs => missJudgeInfo.thresholdMs;

    // 실시간 판정 결과 이벤트 (UI 연출용)
    public event Action<string, int, Color> OnJudgeGenerated;

    // 집계용 내부 데이터
    private float totalAccuracyWeightSum;
    private int maxCombo;
    private int currentCombo;
    private int missCount;

    /// <summary>
    /// 새로운 채보가 시작될 때 데이터를 초기화합니다.
    /// </summary>
    public void Initialize()
    {
        totalAccuracyWeightSum = 0;
        maxCombo = 0;
        currentCombo = 0;
        missCount = 0;
    }

    /// <summary>
    /// 입력이 들어왔을 때 해당 레인의 가장 앞 노트를 대상으로 판정을 수행합니다.
    /// </summary>
    public void ProcessJudgment(int lane, float currentMs, Queue<Note> laneQueue)
    {
        if (laneQueue.Count == 0) return;

        Note targetNote = laneQueue.Peek();
        float diff = Mathf.Abs(targetNote.Data.timeMs - currentMs);

        bool judged = false;
        foreach (var judge in judgeSettings)
        {
            if (diff <= judge.thresholdMs)
            {
                ApplyHit(judge, laneQueue);
                judged = true;
                break;
            }
        }

        // 어떤 판정 범위에도 들지 않을 정도로 너무 빨리 누른 경우는 무시하거나 
        // 필요에 따라 'Early Miss' 처리를 할 수 있습니다.
    }

    /// <summary>
    /// 노트를 처리하지 못하고 지나쳤을 때 호출됩니다.
    /// </summary>
    public void ProcessMiss(Queue<Note> laneQueue)
    {
        ApplyHit(missJudgeInfo, laneQueue);
    }

    private void ApplyHit(JudgeInfo judge, Queue<Note> laneQueue)
    {
        // 1. 결과 집계
        totalAccuracyWeightSum += judge.accuracyWeight;

        if (judge.accuracyWeight > 0) // 성공적인 판정인 경우
        {
            currentCombo++;
            maxCombo = Mathf.Max(maxCombo, currentCombo);
        }
        else // Miss인 경우
        {
            currentCombo = 0;
            missCount++;
        }

        // 2. 물리적 처리 및 이벤트 발행
        Note note = laneQueue.Dequeue();
        OnJudgeGenerated?.Invoke(judge.name, currentCombo, judge.displayColor);

        // 콘솔 디버깅용
        Debug.Log($"[Judge] {judge.name} | Combo: {currentCombo} | Offset: {Mathf.Abs(note.Data.timeMs - (note.Data.timeMs + (judge.accuracyWeight > 0 ? 0 : judge.thresholdMs)))}ms");
        
        // 3. 노트 오브젝트 풀 반환 (Controller에게 맡기거나 직접 참조)
        // 여기서는 Controller의 activeNotes에서도 제거될 수 있도록 처리 흐름을 맞춥니다.
        note.gameObject.SetActive(false); 
    }

    /// <summary>
    /// 턴 매니저에게 전달할 최종 결과 구조체를 생성합니다.
    /// </summary>
    public RhythmResult GetFinalResult(int totalNotesCount)
    {
        return new RhythmResult
        {
            totalAccuracy = totalNotesCount > 0 ? totalAccuracyWeightSum / totalNotesCount : 0f,
            maxCombo = maxCombo,
            missCount = missCount
        };
    }
}

[System.Serializable]
public struct JudgeInfo
{
    public string name;
    public float thresholdMs;
    public float accuracyWeight;
    public Color displayColor; // UI에 표시할 색상
}