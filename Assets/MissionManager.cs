using System.Collections.Generic;
using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MissionManager : MonoBehaviour
{
    List<GameObject> allItemCrates = new();
    List<GameObject> allDroppedItems = new();
    
    public float pickUpDistance;

    AsyncOperation sceneLoadingOperation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach(var obj in GameObject.FindGameObjectsWithTag("Crate"))
            allItemCrates.Add(obj);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            if(sceneLoadingOperation == null) StartLoadingNewScene(1);
            else if(sceneLoadingOperation.progress >= 0.9f && !sceneLoadingOperation.isDone) sceneLoadingOperation.allowSceneActivation = true;
            else if (sceneLoadingOperation.isDone)
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(0));
                SceneManager.UnloadSceneAsync(0);
            }
        }

        if(MenuManager.IsPaused) return;

        //Gets the closest crate, checks if it is closer than pickUpDistance and pressing the pick up button
        GameObject closestCrate = Lists.GetClosest(allItemCrates, player.gameObject);
        if(closestCrate && Vector2.Distance(closestCrate.transform.position, player.position) < pickUpDistance && Input.GetKeyDown(KeyCode.E))
        {
            //Creates an item, adds it to droppeditems, sets parent to world canvas, and sets position
            GameObject item = Instantiate(prefabs["Item"]);
            allDroppedItems.Add(item);
            item.transform.SetParent(GameObject.Find("World Canvas").transform);
            item.transform.position = closestCrate.transform.position + Vector3.up * .5f;

            item.name = "Reverse Gravity";

            //ADD FUNCTIONALITY FOR CHANGING THE IMAGE WHEN KEVIN MAKES THE IMAGES

            //Deletes the crate
            allItemCrates.Remove(closestCrate);
            Destroy(closestCrate);
            return;
        }
        
        //Gets the closest item, checks if it is closer than pickUpDistance and pressing the pick up button
        GameObject closestItem = Lists.GetClosest(allDroppedItems, player.gameObject);
        if(closestItem && Vector2.Distance(closestItem.transform.position, player.position) < pickUpDistance && Input.GetKeyDown(KeyCode.E))
        {
            GameObject item = Instantiate(prefabs[closestItem.name]);

            //Sets the instantiated item to the correct inventory parent
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
            else throw new System.Exception($"Prefab with name {closestItem.name} not found or does not have DragDrop!");

            //Deletes the dropped item
            allDroppedItems.Remove(closestItem);
            Destroy(closestItem);
            return;
        }
    }

    void StartLoadingNewScene(int buildIndex)
    {
        sceneLoadingOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
        sceneLoadingOperation.allowSceneActivation = false;
    }
}
