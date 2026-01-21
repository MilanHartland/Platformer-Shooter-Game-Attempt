using System.Collections.Generic;
using MilanUtils;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ItemCrateBehaviour : MonoBehaviour
{
    [Tooltip("The chance of getting an itemObj instead of ore"), Range(0f, 1f)]public float itemChance;
    [Tooltip("The chance of getting an itemObj instead of ore"), Range(0f, 1f)]public float weaponChance;
    bool canRollItem => itemChance > 0f || weaponChance > 0f;
    bool canRollNotItem => itemChance < 1f || weaponChance < 1f;
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

            if(Input.GetKeyUp(Keybinds.interact)) OpenCrate();
        }
        else interactTextObject.SetActive(false);
    }

    public void OpenCrate()
    {
        int oreCount = Random.Range(loneOreCount.min, loneOreCount.max);;

        //If the random chance passes, spawn item. Otherwise, only increase ore
        bool spawnItem = Random.value <= itemChance;
        bool spawnWeapon = Random.value <= weaponChance;
        if (spawnWeapon)
        {
            List<string> newList = new(ModuleApplyHandler.allWeapons.Keys.ToList());
            foreach(Transform parent in World.FindInactive("Weapon Inventory").transform)
            {
                foreach(Transform child in parent)
                {
                    if(child.TryGetComponent(out WeaponInfo wi))
                        newList.Remove(wi.name);
                }
            }

            if(newList.Count > 0)
            {
                //Gets a random item name from the list
                string itemName = newList[Random.Range(0, newList.Count)];
                
                //Creates an itemObj, adds it to droppeditems, sets parent to world canvas, and sets position
                GameObject itemObj = Instantiate(Variables.prefabs[itemName]);
                MissionManager.allDroppedItems.Add(itemObj);
                itemObj.transform.SetParent(GameObject.Find("World Canvas").transform);
                itemObj.transform.position = transform.position;
                itemObj.GetComponent<RectTransform>().sizeDelta = Vector3.one;

                itemObj.name = $"{itemName} Dropped Item";

                itemObj.GetComponent<Image>().sprite = Variables.prefabs[itemName].GetComponent<Image>().sprite;

                oreCount = Random.Range(itemOreCount.min, itemOreCount.max);
            }
        }
        else if (spawnItem)
        {
            List<string> newList = new(ModuleApplyHandler.allItems.Keys.ToList());
            foreach(Transform item in World.FindInactive("Item Inventory").transform)
            {
                if(item.TryGetComponent(out ItemInfo ii))
                {
                    newList.Remove(ii.name);
                }
            }

            if(newList.Count > 0)
            {
                //Gets a random item name from the list
                string itemName = newList[Random.Range(0, newList.Count)];
                
                //Creates an itemObj, adds it to droppeditems, sets parent to world canvas, and sets position
                GameObject itemObj = Instantiate(Variables.prefabs[itemName]);
                MissionManager.allDroppedItems.Add(itemObj);
                itemObj.transform.SetParent(GameObject.Find("World Canvas").transform);
                itemObj.transform.position = transform.position;
                itemObj.GetComponent<RectTransform>().sizeDelta = Vector3.one;

                itemObj.name = $"{itemName} Dropped Item";

                itemObj.GetComponent<Image>().sprite = Variables.prefabs[itemName].GetComponent<Image>().sprite;

                oreCount = Random.Range(itemOreCount.min, itemOreCount.max);
            }
        }

        //Spawns the ore text and increases PlayerManager oreCount
        FloatingText.SpawnOreText(gameObject, oreCount);
        PlayerManager.oreCount += oreCount;
        
        //Destroys the text object and disintegrates this object
        Destroy(interactTextObject);
        Effects.Disintegrate(gameObject, dontThrowNonReadException: true);
        return;
    }
}
