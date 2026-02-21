using UnityEngine;

public class Note : MonoBehaviour
{
    public NoteData Data { get; private set; }
    public RectTransform Rect { get; private set; }

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
    }

    public void Setup(NoteData data)
    {
        Data = data;
    }
}