using System;
using UnityEngine;

public class RhythmLogicController : MonoBehaviour
{
    // 게임 정상 종료 시 결과를 전달할 이벤트
    public event Action<RhythmResult> OnGameFinished;

    public void InitializeGameData(string chartID, bool isAttackTurn)
    {
        // 채보 데이터 로드 및 전체 노트 수 캐싱 로직 대기 위치
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

    public void PauseSystem()
    {
        // Time.timeScale = 0 및 dspTime 정지 로직 대기 위치
    }

    public void ResumeSystem()
    {
        // dspTime 오프셋 보정 및 재생 재개 로직 대기 위치
    }
}