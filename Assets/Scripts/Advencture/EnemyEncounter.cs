using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EnemyEncounter : MonoBehaviour
{
    [Header("Encounter Settings")]
    [Tooltip("플레이어와 부딪혔을 때 전투 씬에 등장시킬 적 프리팹들을 넣으세요.")]
    public List<GameObject> enemiesToSpawn; 
    
    [Header("Scene Routing")]
    public string lobbySceneName = "LobbyScene"; // 부딪히면 이동할 캐릭터 선택 창 이름

    // 2D 게임일 경우 (3D 게임이라면 OnTriggerEnter(Collider other) 로 변경하십시오)
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 캐릭터와 부딪혔는지 검사
        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=red>[시스템] 적과 조우했습니다! 로비로 이동합니다.</color>");
            
            // 1. 전투에 튀어나올 적 프리팹 명단을 게임 매니저에 저장
            if (GameManager.Instance != null)
            {
                GameManager.Instance.encounteredEnemyPrefabs = new List<GameObject>(enemiesToSpawn);
            }
            
            // 2. 캐릭터 선택 로비 씬으로 이동
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}