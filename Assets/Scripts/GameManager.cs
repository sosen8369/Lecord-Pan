using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{// 어디서든 GameManager.Instance로 접근할 수 있게 만드는 싱글톤 패턴
    public static GameManager Instance { get; private set; }

    [Header("Character Roster (전체 6명 캐릭터 프리팹)")]
    public List<GameObject> allCharacterPrefabs;

    [Header("Selected Party (전투에 나갈 4명)")]
    public List<GameObject> selectedPartyPrefabs = new List<GameObject>();

    [Header("Encountered Enemy (전투에 등장할 적군)")]
    public List<GameObject> encounteredEnemyPrefabs = new List<GameObject>();

    private void Awake()
    {
        // 씬이 넘어가도 파괴되지 않도록 락을 겁니다.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // [임시 방어 코드] 캐릭터 선택 UI가 아직 없으므로, 비어있으면 전체 명단 중 앞의 4명을 억지로 구겨 넣습니다.
        /*
        if (selectedPartyPrefabs.Count == 0 && allCharacterPrefabs.Count >= 4)
        {
            Debug.LogWarning("<color=yellow>[GameManager] 선택된 파티가 없어 자동으로 1~4번 캐릭터를 편성합니다.</color>");
            for (int i = 0; i < 4; i++)
            {
                selectedPartyPrefabs.Add(allCharacterPrefabs[i]);
            }
        }
        */
        
    }
}
