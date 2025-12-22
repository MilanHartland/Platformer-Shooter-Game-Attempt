using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using MilanUtils;
using System.Linq;

public class MissionSelection : MonoBehaviour
{
    [Serializable]
    public class Mission
    {
        public int buildIndex;
        public ExclusiveItem<Button> exclusiveButton;
        [Space]
        public string name;
        public int difficulty;
        public int size;
        public int loot;
    }

    public List<Mission> missions;

    Mission hoveredMission;
    Mission selectedMission;

    [Space]
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI sizeText;
    public TextMeshProUGUI valueText;
    public Button selectButton;

    [Space]
    public Bounds _editorCameraTakeOverArea;
    Bounds cameraTakeOverArea;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selectedMission = missions[0];

        selectButton.onClick.AddListener(() => 
        {
            MissionManager.sceneIndexToLoad = selectedMission.buildIndex;
        });

        cameraTakeOverArea = new(transform.position, _editorCameraTakeOverArea.size);
        cameraTakeOverArea.size += Vector3.forward * 999f;

        GenerateMapLayout();
    }

    // Update is called once per frame
    void Update()
    {
        hoveredMission = selectedMission;
        selectButton.interactable = !(selectedMission.buildIndex == MissionManager.sceneIndexToLoad);

        //When hovering over none, show selected item. When hovering over something, show it. If clicked, select it
        foreach(var obj in UI.GetObjectsUnderMouse())
        {
            foreach(var mission in missions)
            {
                if(mission.exclusiveButton.item.gameObject == obj) 
                {
                    selectButton.interactable = !(mission.buildIndex == MissionManager.sceneIndexToLoad);
                    hoveredMission = mission;

                    if(Input.GetMouseButtonDown(0)) selectedMission = mission;
                    break;
                }
            }
        }
        
        if(cameraTakeOverArea.Contains(Variables.player.position)) Camera.main.GetComponent<CameraFollow>().ImportFollowProfile(CameraFollow.allProfiles["Map View Profile"]);
        else if(!MissionManager.isTransitioning && !MissionManager.inGunBenchArea) Camera.main.GetComponent<CameraFollow>().ImportFollowProfile(CameraFollow.allProfiles["Hub Profile"]);

        mapNameText.text = hoveredMission.name;
        difficultyText.text = $"Difficulty: {hoveredMission.difficulty}";
        sizeText.text = $"Size: {hoveredMission.size}";
        valueText.text = $"Loot: {hoveredMission.loot}";
    }

    void GenerateMapLayout()
    {
        ExclusiveList<Button> exList = new(new());
        foreach(var mission in missions){exList.Add(mission.exclusiveButton);}
        
        List<Button> chosenButtons = exList.GetRandomCombination(exList.GetBiggestCombinationSize());
        List<Mission> chosenMissions = new();
        foreach(var obj in missions)
        {
            if(chosenButtons.Contains(obj.exclusiveButton.item)) 
            {
                chosenMissions.Add(obj);
                obj.exclusiveButton.item.gameObject.SetActive(true);
            }
            else obj.exclusiveButton.item.gameObject.SetActive(false);
        }
    }

    void OnValidate()
    {
        cameraTakeOverArea = new(transform.position, _editorCameraTakeOverArea.size);
        cameraTakeOverArea.size += Vector3.forward * 999f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(cameraTakeOverArea.center, cameraTakeOverArea.size);
    }
}
