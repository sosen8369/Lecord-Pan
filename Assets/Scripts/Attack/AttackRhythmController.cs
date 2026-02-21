using UnityEngine;
using System.Collections.Generic;
using System;

public class AttackRhythmController : MonoBehaviour
{
    public event Action<RhythmResult> OnAttackFinished;

    [Header("Managers")]
    [SerializeField] private JudgmentManager judgmentManager;
    [SerializeField] private ObjectPoolManager poolManager;

    [Header("UI References")]
    [SerializeField] private RectTransform spawnPoint;   // 좌측 시작 지점
    [SerializeField] private RectTransform targetLine;   // 우측 타겟(판정) 지점

    [Header("Settings")]
    [SerializeField] private float leadTimeMs = 1500f;   // 스폰 후 타겟까지 도달하는 시간

    private List<Note> activeBars = new List<Note>();
    private Queue<Note> barQueue = new Queue<Note>();
    
    private float[] attackTimingsMs;
    private int nextSpawnIndex = 0;
    private float currentMs = 0f;
    private bool isPlaying = false;
    private int totalBars = 0;

    // 다중 공격 타이밍 배열을 인자로 받아 공격 세션을 시작합니다.
    public void StartAttack(float[] timingsMs)
    {
        attackTimingsMs = timingsMs;
        totalBars = timingsMs.Length;
        nextSpawnIndex = 0;
        currentMs = 0f;
        
        activeBars.Clear();
        barQueue.Clear();
        judgmentManager.Initialize();
        
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying) return;

        currentMs += Time.deltaTime * 1000f;

        while (nextSpawnIndex < totalBars && 
               currentMs + leadTimeMs >= attackTimingsMs[nextSpawnIndex])
        {
            SpawnBar(attackTimingsMs[nextSpawnIndex]);
            nextSpawnIndex++;
        }

        UpdateBarPositions();
        CheckMissBars();
        CheckEndCondition();
    }

    private void SpawnBar(float targetTimeMs)
    {
        GameObject barObj = poolManager.GetFromPool(0, spawnPoint.parent); 
        Note noteComponent = barObj.GetComponent<Note>();
        
        // 델타룬 방식은 단일 버튼(레인 0)을 사용합니다.
        NoteData data = new NoteData { timeMs = targetTimeMs, lane = 0, type = 0 };
        noteComponent.Setup(data);
        noteComponent.Rect.localScale = Vector3.one;
        noteComponent.Rect.position = spawnPoint.position;

        activeBars.Add(noteComponent);
        barQueue.Enqueue(noteComponent);
    }

    private void UpdateBarPositions()
    {
        for (int i = activeBars.Count - 1; i >= 0; i--)
        {
            Note bar = activeBars[i];
            
            if (!bar.gameObject.activeSelf)
            {
                activeBars.RemoveAt(i);
                poolManager.ReturnToPool(bar.Data.type, bar.gameObject);
                continue;
            }

            float timeDiff = bar.Data.timeMs - currentMs;
            
            // X축 기반 횡이동 공식 적용 (t=1: 시작점, t=0: 판정선)
            float t = timeDiff / leadTimeMs;
            Vector3 newPos = Vector3.LerpUnclamped(targetLine.position, spawnPoint.position, t);
            
            bar.Rect.position = new Vector3(newPos.x, spawnPoint.position.y, spawnPoint.position.z);
        }
    }

    public void OnInput()
    {
        if (!isPlaying || barQueue.Count == 0) return;
        judgmentManager.ProcessJudgment(0, currentMs, barQueue);
    }

    private void CheckMissBars()
    {
        if (barQueue.Count > 0)
        {
            Note frontBar = barQueue.Peek();
            if (currentMs - frontBar.Data.timeMs > judgmentManager.MissThresholdMs)
            {
                judgmentManager.ProcessMiss(barQueue);
            }
        }
    }

    private void CheckEndCondition()
    {
        if (nextSpawnIndex >= totalBars && activeBars.Count == 0)
        {
            isPlaying = false;
            RhythmResult result = judgmentManager.GetFinalResult(totalBars);
            OnAttackFinished?.Invoke(result);
        }
    }
}