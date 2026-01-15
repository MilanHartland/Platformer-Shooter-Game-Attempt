using System.Collections.Generic;
using MilanUtils;
using UnityEngine;
using TMPro;

public class ItemCrateBehaviour : MonoBehaviour
{
    [Tooltip("The quality of crate. Used to determine which loot pool to use")]public ItemInfo.ItemQuality crateQuality;
    [Tooltip("The chance of getting an itemObj instead of ore"), Range(0f, 1f)]public float itemChance;
    bool canRollItem => itemChance > 0f;
    bool canRollNotItem => itemChance < 1f;
    [Tooltip("The range of ore that is possible to get when no itemObj is rolled"), ShowIf("canRollNotItem")]public IntRange loneOreCount;
    [Tooltip("The range of ore that is possible to get when an itemObj is rolled"), ShowIf("canRollItem")]public IntRange itemOreCount;

    GameObject interactTextObject;

    void OnValidate()
    {
        if(loneOreCount.min < 0) loneOreCount.min = 0;
        if(loneOreCount.min > loneOreCount.max) loneOreCount.max = loneOreCount.min;
        if(itemOreCount.min < 0) itemOreCount.min = 0;
        if(itemOreCount.min > itemOreCount.max) itemOreCount.max = itemOreCount.min;
    }

    void Start()
    {
        interactTextObject = Instantiate(Variables.prefabs["Crate Interact Text"], GameObject.Find("World Canvas").transform);
        interactTextObject.transform.position = transform.position + Vector3.up * 2f;
        interactTextObject.GetComponent<TextMeshProUGUI>().text = $"[{Keybinds.interact}] Open Crate";
    }

    // Update is called once per frame
    void Update()
    {
        //If within interact distance, set interact text object to active, and if pressing interact open the crate. Otherwise, disable the text object
        if(Vector2.Distance(transform.position, Variables.player.position) < 2f)
        {
            interactTextObject.SetActive(true);

            if(Input.GetKeyDown(Keybinds.interact)) OpenCrate();
        }
        else interactTextObject.SetActive(false);
    }

    public void OpenCrate()
    {
        int oreCount;

        //If the random chance passes, spawn item. Otherwise, only increase ore
        bool spawnItem = Random.value <= itemChance;
        if (spawnItem)
        {
            //Gets all items of the correct rarity from ModuleApplyHandler
            List<ItemInfo> allRarityItems = new();
            foreach(var obj in ModuleApplyHandler.allItems.Values)
            {
                if(obj.itemQuality == crateQuality) allRarityItems.Add(obj);
            }

            //Gets a random item name from the list
            string itemName = allRarityItems[Random.Range(0, allRarityItems.Count)].name;
            
            //Creates an itemObj, adds it to droppeditems, sets parent to world canvas, and sets position
            GameObject itemObj = Instantiate(Variables.prefabs[itemName]);
            MissionManager.allDroppedItems.Add(itemObj);
            itemObj.transform.SetParent(GameObject.Find("World Canvas").transform);
            itemObj.transform.position = transform.position;

            itemObj.name = $"{itemName} Dropped Item";

            //ADD FUNCTIONALITY FOR CHANGING THE IMAGE WHEN KEVIN MAKES THE IMAGES

            oreCount = Random.Range(itemOreCount.min, itemOreCount.max);
        }
        else
            oreCount = Random.Range(loneOreCount.min, loneOreCount.max);

        //Spawns the ore text and increases PlayerManager oreCount
        FloatingText.SpawnOreText(gameObject, oreCount);
        PlayerManager.oreCount += oreCount;
        
        //Destroys the text object and disintegrates this object
        Destroy(interactTextObject);
        Effects.Disintegrate(gameObject, dontThrowNonReadException: true);
        return;
    }
}
