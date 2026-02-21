[System.Serializable]
public class RhythmGameChart
{
    public string title;
    public string artist;
    public float audioOffsetMs;
    public BpmEvent[] bpmEvents;
    public SpeedEvent[] speedEvents;
    public NoteData[] notes;
}

[System.Serializable]
public class BpmEvent
{
    public float timeMs;
    public float bpm;
}

[System.Serializable]
public class SpeedEvent
{
    public float timeMs;
    public float multiplier;
}

[System.Serializable]
public class NoteData
{
    public int type;       // 0: Tap, 1: Hold
    public float timeMs;
    public int lane;       // 0 ~ 3
    public float duration; // Hold 노트용, Tap일 경우 0
}