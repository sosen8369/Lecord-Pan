using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PartySetupUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button[] rosterButtons;       
    public Image[] selectedSlotImages;   
    public Sprite emptySlotSprite;       
    public Button startBattleButton;     

    [Header("Scene Settings")]
    public string battleSceneName = "BattleScene";

    private List<GameObject> currentSelection = new List<GameObject>();

    private void Start()
    {
        for (int i = 0; i < rosterButtons.Length; i++)
        {
            int index = i;
            rosterButtons[i].onClick.AddListener(() => OnRosterClicked(index));
        }

        startBattleButton.onClick.AddListener(StartBattle);
        
        // ★ 새로 추가: 씬 시작 시 전체 로스터 버튼들에 캐릭터 초상화를 입혀줍니다.
        InitRosterButtons(); 
        
        UpdateUI();
    }

    // --- 하단 6개 버튼에 캐릭터 이미지를 입히는 초기화 로직 ---
    private void InitRosterButtons()
    {
        if (GameManager.Instance == null) return;

        for (int i = 0; i < rosterButtons.Length; i++)
        {
            if (i < GameManager.Instance.allCharacterPrefabs.Count)
            {
                GameObject prefab = GameManager.Instance.allCharacterPrefabs[i];
                BattleUnit unit = prefab.GetComponent<BattleUnit>();

                if (unit != null && unit.characterPortrait != null)
                {
                    // 버튼이 기본적으로 가지고 있는 Image 컴포넌트에 초상화 덮어쓰기
                    Image btnImage = rosterButtons[i].GetComponent<Image>();
                    if (btnImage != null)
                    {
                        btnImage.sprite = unit.characterPortrait;
                    }
                    
                    // 만약 버튼 하위에 텍스트가 있다면 지저분하지 않게 이름을 덮어쓰거나 비웁니다.
                    Text btnText = rosterButtons[i].GetComponentInChildren<Text>();
                    if (btnText != null) btnText.text = ""; 
                }
            }
            else
            {
                // 준비된 프리팹 수보다 버튼 갯수가 많으면 잉여 버튼은 화면에서 아예 꺼버립니다.
                rosterButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnRosterClicked(int index)
    {
        if (GameManager.Instance == null) return;
        if (index >= GameManager.Instance.allCharacterPrefabs.Count) return;

        GameObject selectedPrefab = GameManager.Instance.allCharacterPrefabs[index];

        if (currentSelection.Contains(selectedPrefab))
        {
            currentSelection.Remove(selectedPrefab);
        }
        else if (currentSelection.Count < selectedSlotImages.Length)
        {
            currentSelection.Add(selectedPrefab);
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 1. 위쪽 4개의 선택 슬롯 이미지 갱신
        for (int i = 0; i < selectedSlotImages.Length; i++)
        {
            if (i < currentSelection.Count)
            {
                BattleUnit unit = currentSelection[i].GetComponent<BattleUnit>();
                if (unit != null && unit.characterPortrait != null)
                {
                    selectedSlotImages[i].sprite = unit.characterPortrait;
                    selectedSlotImages[i].color = Color.white; 
                }
            }
            else
            {
                if (emptySlotSprite != null)
                {
                    selectedSlotImages[i].sprite = emptySlotSprite;
                    selectedSlotImages[i].color = Color.white;
                }
                else
                {
                    selectedSlotImages[i].sprite = null;
                    selectedSlotImages[i].color = Color.clear; 
                }
            }
        }

        // 2. ★ 새로 추가: 하단 로스터 버튼들의 선택 여부 피드백 (어둡게 만들기)
        if (GameManager.Instance != null)
        {
            for (int i = 0; i < rosterButtons.Length; i++)
            {
                if (i < GameManager.Instance.allCharacterPrefabs.Count)
                {
                    GameObject prefab = GameManager.Instance.allCharacterPrefabs[i];
                    Image btnImage = rosterButtons[i].GetComponent<Image>();
                    
                    if (btnImage != null)
                    {
                        // 이미 파티에 들어간 프리팹이라면 버튼을 어둡게(회색) 처리
                        if (currentSelection.Contains(prefab))
                            btnImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
                        else
                            btnImage.color = Color.white; // 안 골랐으면 원래 밝기로
                    }
                }
            }
        }

        // 3. 시작 버튼 활성화 제어
        startBattleButton.interactable = currentSelection.Count > 0;
    }

    private void StartBattle()
    {
        GameManager.Instance.selectedPartyPrefabs = new List<GameObject>(currentSelection);
        SceneManager.LoadScene(battleSceneName);
    }
}