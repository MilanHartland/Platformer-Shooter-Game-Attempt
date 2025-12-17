using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MissionSelection : MonoBehaviour
{
    [System.Serializable]
    public class Mission
    {
        public int buildIndex;
        [Space]
        public string name;
        public int difficulty;
        public int size;
        public int value;
    }

    public List<Mission> missions;

    Mission selectedMission;

    [Space]
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI sizeText;
    public TextMeshProUGUI valueText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selectedMission = missions[0];
    }

    // Update is called once per frame
    void Update()
    {
        mapNameText.text = selectedMission.name;
        difficultyText.text = $"Difficulty: {selectedMission.difficulty}";
        sizeText.text = $"Size: {selectedMission.size}";
        valueText.text = $"Value: {selectedMission.value}";
    }
}
