using System.Collections.Generic;
using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    List<GameObject> allItemCrates = new();
    List<GameObject> allDroppedItems = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach(var obj in GameObject.FindGameObjectsWithTag("Crate"))
            allItemCrates.Add(obj);
    }

    // Update is called once per frame
    void Update()
    {
        if(MenuManager.IsPaused) return;

        GameObject closestCrate = Lists.GetClosest(allItemCrates, player.gameObject);
        if(closestCrate && Vector2.Distance(closestCrate.transform.position, player.position) < 2f && Input.GetKeyDown(KeyCode.E))
        {
            GameObject crate = Instantiate(prefabs["Item"]);
            allDroppedItems.Add(crate);
            crate.transform.SetParent(GameObject.Find("World Canvas").transform);
            crate.transform.position = closestCrate.transform.position + Vector3.up * .5f;

            crate.name = "Reverse Gravity";

            //ADD FUNCTIONALITY FOR CHANGING THE IMAGE WHEN KEVIN MAKES THE IMAGES

            allItemCrates.Remove(closestCrate);
            Destroy(closestCrate);
            return;
        }

        GameObject closestItem = Lists.GetClosest(allDroppedItems, player.gameObject);
        if(closestItem && Vector2.Distance(closestItem.transform.position, player.position) < 2f && Input.GetKeyDown(KeyCode.E))
        {
            GameObject item = Instantiate(prefabs[closestItem.name]);

            if (item && item.GetComponent<DragDrop>())
            {
                if(item.GetComponent<DragDrop>().name == "Bullet")
                {
                    item.transform.SetParent(World.FindInactive("Bullet Inventory").transform);
                }
                else if(item.GetComponent<DragDrop>().name == "Effect")
                {
                    item.transform.SetParent(World.FindInactive("Effect Inventory").transform);
                }

                item.transform.localScale = Vector3.one;
            }
            else throw new System.Exception("Prefab with item name not found or does not have DragDrop");

            allDroppedItems.Remove(closestItem);
            Destroy(closestItem);
            return;
        }
    }
}
