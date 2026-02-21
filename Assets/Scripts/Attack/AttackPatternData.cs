using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackPattern", menuName = "Rhythm/Attack Pattern")]
public class AttackPatternData : ScriptableObject
{
    public string patternName;
    public float startMarginMs; // 공격 시작 전 대기 시간 (밀리초)
    public float[] relativeTimingsMs; // margin 이후 추가되는 개별 막대 도달 시간 (밀리초)
}