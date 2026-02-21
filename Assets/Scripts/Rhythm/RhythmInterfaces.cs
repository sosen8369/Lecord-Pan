using System.Threading;
using UnityEngine;

// 턴 매니저로 반환할 결과 데이터 구조체
public struct RhythmResult
{
    public float totalAccuracy;
    public int maxCombo;
    public int missCount;
}

// 턴 매니저와 통신하기 위한 인터페이스 규약
public interface IRhythmSystem
{
    Awaitable<RhythmResult> PlayRhythmGameAsync(string chartID, bool isAttackTurn, CancellationToken cancellationToken);
    void PauseRhythm();
    void ResumeRhythm();
}