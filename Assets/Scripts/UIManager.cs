using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI hp;
    
    [SerializeField]
    private TextMeshProUGUI cp;

    [SerializeField]
    private TurnManager turnManager;
    void Start()
    {

        hp.text = "HP:";
        cp.text = "CP:";
    }

    // Update is called once per frame
    void Update()
    {
        
        hp.text = "HP: " + turnManager.partyCurrentHP;
        cp.text = "CP: " + turnManager.partyCurrentChorus;
    }
}
