using System.Collections.Generic;
using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine;
using System.Collections;
using static UnityEngine.ParticleSystem;
using UnityEngine.SceneManagement;
using System;
using TMPro;

public class MissionManager : MonoBehaviour
{
    List<GameObject> allItemCrates = new();
    List<GameObject> allDroppedItems = new();
    GameObject depositBox, lidMask, gunBench;
    Vector3 sceneEnterPoint, playerSpawnPoint, boxTeleportPoint;
    float boxStopHeight;
    
    [Header("Item Pickups")]
    public float pickUpDistance;
    public Bounds depositBoxPickupArea; //Can't be SetName'd because the GetHeight doesn't work for Bounds
    Bounds pickupArea;

    GameObject depositBoxButtonUI;

    [Serializable]
    public struct TransitionVariables
    {
        [Header("Deposit Box Transition")]
        [Tooltip("The amount of seconds it takes for the gun to move/scale/rotate into position")]public float gunMoveTime;
        [Tooltip("The amount of seconds it takes between the gun being in position and the lid closing")]public float gunLidPauseTime;
        [Tooltip("The amount of seconds it takes for the lid to close")]public float lidMoveTime;
        [Tooltip("The amount of seconds it takes between the lid being closed and the box moving")]public float lidBoxPauseTime;
        [Tooltip("The acceleration the box undergoes when going to a new scene")]public float boxAcceleration;
        [Tooltip("The deceleration the box undergoes when having entered a new scene")]public float boxDeceleration;
        [Tooltip("The max speed of the box")]public float boxMaxSpeed;
        [Tooltip("The amount of seconds it takes for the box to move 1 unit down at the end")]public float boxHideTime;
        
        [Header("Respawn Transition")]
        [Tooltip("The time it takes for the camera to fly up")]public float camFlyTime;
        [Tooltip("The distance the camera flies up")]public float camFlyHeight;
        [Tooltip("The time it takes for the camera to settle on the Hub scene")]public float camStopTime;
    }
    [Header("Scene Transitions")]
    public TransitionVariables transition;
    float sceneChangeCamTriggerHeight;

    AsyncOperation sceneLoadingOperation;
    public static int sceneIndexToLoad;

    public static bool inHub;
    public static bool isTransitioning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneManager.activeSceneChanged += GetSceneObjects;
        GetSceneObjects(default, default);

        inHub = SceneManager.GetActiveScene().name.Contains("Hub");
        isTransitioning = false;

        depositBox = GameObject.Find("Deposit Box");
        lidMask = depositBox.transform.Find("Lid Mask").gameObject;
        lidMask.SetActive(false);
    }

    private void GetSceneObjects(Scene a, Scene b)
    {
        foreach(var obj in GameObject.FindGameObjectsWithTag("Crate"))
            allItemCrates.Add(obj);

        sceneEnterPoint = GameObject.Find("Scene Enter Point").transform.position;
        sceneChangeCamTriggerHeight = GameObject.Find("Scene Exit Point").transform.position.y;
        playerSpawnPoint = GameObject.Find("Player Spawn Point").transform.position;
        boxStopHeight = GameObject.Find("Deposit Box Stop Point").transform.position.y;
        boxTeleportPoint = GameObject.Find("Deposit Box Teleport Point").transform.position;

        depositBoxButtonUI = GameObject.Find("Deposit Box Use");

        if(SceneManager.GetActiveScene().name.Contains("Mission")) 
        {
            inHub = false;
            sceneIndexToLoad = 1;
        }
        else if(SceneManager.GetActiveScene().name.Contains("Hub")) 
        {
            inHub = true;
            sceneIndexToLoad = 2; //When adding more missions, change this to make it unable to load anything until mission is selected
        }
        else throw new Exception($"No scene found to get! Current scene is {SceneManager.GetActiveScene().name}");

        if(isTransitioning) SceneTransitionFunction();
    }

    private void SceneTransitionFunction()
    {
        player.gameObject.SetActive(true);
        player.position = playerSpawnPoint;
        player.transform.Find("Gun").gameObject.SetActive(false);
        
        Vector3 posDiff = depositBox.transform.position - Camera.main.transform.position;
        Camera.main.transform.position = sceneEnterPoint + Vector3.back * 10f;
        depositBox.transform.position = sceneEnterPoint + posDiff;
    }

    // Update is called once per frame
    void Update()
    {
        if(MenuManager.IsPaused) return;

        //Sets the pickupArea position to center in relation to depositBox position
        pickupArea = new(depositBox.transform.position + depositBoxPickupArea.center, depositBoxPickupArea.size);

        depositBoxButtonUI.SetActive(pickupArea.Contains(player.position) && !isTransitioning);
        if (pickupArea.Contains(player.position))
        {
            if(inHub) depositBoxButtonUI.GetComponent<TextMeshProUGUI>().text = "[E] Deploy";
            else depositBoxButtonUI.GetComponent<TextMeshProUGUI>().text = "[E] Extract";
        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            //Gets the closest crate, checks if it is closer than pickUpDistance and pressing the pick up button
            GameObject closestCrate = Lists.GetClosest(allItemCrates, player.gameObject);
            if(closestCrate && Vector2.Distance(closestCrate.transform.position, player.position) < pickUpDistance)
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
            if(closestItem && Vector2.Distance(closestItem.transform.position, player.position) < pickUpDistance)
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
                else throw new Exception($"Prefab with name {closestItem.name} not found or does not have DragDrop!");

                //Deletes the dropped item
                allDroppedItems.Remove(closestItem);
                Destroy(closestItem);
                return;
            }

            if (pickupArea.Contains(player.position) && !PlayerManager.isDead)
            {
                StartCoroutine(ExtractionCoroutine());
            }
        }
    }

    public IEnumerator ExtractionCoroutine()
    {
        //Sets the bool to true, which is used in the scene change function
        isTransitioning = true;

        //Gets the rigidbody
        Rigidbody2D rb = depositBox.GetComponent<Rigidbody2D>();

        //Gets the camera follow and sets the profile to the transition profile
        CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
        camFollow.ImportFollowProfile("Transition Profile");
        
        //Loads the scene in the background
        sceneLoadingOperation = SceneManager.LoadSceneAsync(sceneIndexToLoad);
        sceneLoadingOperation.allowSceneActivation = false;

        //Gets the player gun, copies it, then sets name/parent/localscale (with absolute x values)/position, then adds to DDOL
        Transform playerGun = player.transform.Find("Gun");
        Transform gunCopy = Instantiate(playerGun).transform;
        gunCopy.name = "Gun Deposit Copy";
        gunCopy.SetParent(null);
        gunCopy.localScale = player.TransformVector(playerGun.transform.localScale);
        gunCopy.localScale = new Vector3(Mathf.Abs(gunCopy.localScale.x), gunCopy.localScale.y);
        gunCopy.position = playerGun.position;
        DontDestroyOnLoad(gunCopy.gameObject);

        //Gets and disables the animation manager and player movement
        AnimationManager animManager = player.GetComponent<AnimationManager>();
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        animManager.enabled = false;
        playerMovement.enabled = false;

        //Disables the player object and creates a particle copy of it
        player.gameObject.SetActive(false);
        Effects.TurnIntoParticles(player.gameObject, player.position, dontThrowNonReadException: true);
        player.transform.position = Vector3.one * 9999f;

        //Moves the copy to the box, rotates it to have no rotation, and scales it to be bigger
        LeanTween.move(gunCopy.gameObject, depositBox.transform.position, transition.gunMoveTime).setEaseOutQuint();
        LeanTween.rotateZ(gunCopy.gameObject, -90f, transition.gunMoveTime).setEaseInOutSine();
        LeanTween.scale(gunCopy.gameObject, Vector3.one * .5f, transition.gunMoveTime).setEaseInOutSine();

        depositBox.GetComponent<SpriteRenderer>().sortingOrder = 1;
        depositBox.transform.Find("Lid").GetComponent<SpriteRenderer>().sortingOrder = 2;
        
        yield return GetWaitForSeconds(transition.gunMoveTime + transition.gunLidPauseTime);

        //Activates the mask (if already active it would've maybe interacted with the gun) and moves it
        lidMask.SetActive(true);
        LeanTween.move(lidMask, depositBox.transform.position, transition.lidMoveTime).setEaseOutQuint();

        yield return GetWaitForSeconds(transition.lidMoveTime + transition.lidBoxPauseTime);
        gunCopy.gameObject.SetActive(false); //Disables the copy

        //Gets the sign (1 means have to move up, -1 is down). Then, while camera still has to move or loading isn't done, if velocity is under max, accelerate
        float sign = Mathf.Sign(sceneChangeCamTriggerHeight - Camera.main.transform.position.y);
        while(Camera.main.transform.position.y * sign < sceneChangeCamTriggerHeight * sign || sceneLoadingOperation.progress < .9f)
        {
            if(Mathf.Abs(rb.linearVelocityY) < transition.boxMaxSpeed) rb.linearVelocityY += transition.boxAcceleration * Time.deltaTime * sign;
            yield return null;
        }
        
        //Activates the scene, sets linearvelocity to max (if it wasn't already), and unrestricts the camera follow. Then waits a frame to stop a weird scene-transition-jitter thing
        sceneLoadingOperation.allowSceneActivation = true;
        rb.linearVelocityY = transition.boxMaxSpeed * sign;
        camFollow.restrictionType = CameraFollow.RestrictionType.Unrestricted;
        yield return null;

        //Gets the sign again (just to be sure), then while depositbox still has to move and has some speed, if needs to decelerate (physics shit idk), decelerate
        sign = Mathf.Sign(boxStopHeight - depositBox.transform.position.y);
        while(depositBox.transform.position.y * sign < boxStopHeight * sign && Mathf.Abs(rb.linearVelocityY) > 0.1f)
        {
            if(Mathf.Abs(boxStopHeight - depositBox.transform.position.y) < transition.boxMaxSpeed * transition.boxMaxSpeed / (2f * transition.boxDeceleration)) 
                rb.linearVelocityY -= transition.boxDeceleration * Time.deltaTime * sign;
            yield return null;
        }

        //Sets velocity and position perfectly
        rb.linearVelocityY = 0f;
        depositBox.transform.position = new(depositBox.transform.position.x, boxStopHeight);

        yield return GetWaitForSeconds(transition.lidBoxPauseTime);
        
        //Activates the gun copy again and sets position/scale (with the same x scale sign as the actual gun), and activates lidMask
        gunCopy.gameObject.SetActive(true);
        gunCopy.transform.position = depositBox.transform.position;
        gunCopy.localScale = new Vector3(Mathf.Sign(player.TransformVector(playerGun.localScale).x) * gunCopy.localScale.x, gunCopy.localScale.y);
        lidMask.SetActive(true);

        //Moves the lid up again
        LeanTween.move(lidMask.gameObject, depositBox.transform.position + Vector3.up, transition.lidMoveTime).setEaseOutQuint();

        yield return GetWaitForSeconds(transition.lidMoveTime + transition.gunLidPauseTime);

        //Disables the lidMask, then moves/scales/rotates the gun copy to the actual gun
        lidMask.SetActive(false);
        LeanTween.move(gunCopy.gameObject, playerGun.position, transition.gunMoveTime).setEaseOutQuint();
        LeanTween.rotateZ(gunCopy.gameObject, playerGun.eulerAngles.z, transition.gunMoveTime).setEaseInOutSine();
        LeanTween.scale(gunCopy.gameObject, playerGun.lossyScale, transition.gunMoveTime).setEaseInOutSine();

        yield return GetWaitForSeconds(transition.gunMoveTime);

        depositBox.GetComponent<SpriteRenderer>().sortingOrder = -2;
        depositBox.transform.Find("Lid").GetComponent<SpriteRenderer>().sortingOrder = -1;

        LeanTween.moveY(depositBox, depositBox.transform.position.y - 1f, transition.boxHideTime).setEaseInQuart().setOnComplete(() => 
        {
            depositBox.transform.position = boxTeleportPoint;

            depositBox.GetComponent<SpriteRenderer>().sortingOrder = 1;
            depositBox.transform.Find("Lid").GetComponent<SpriteRenderer>().sortingOrder = 2;
        });

        //Reenables the animation/movement, reactivates the gun, and destroys the copy
        animManager.enabled = true;
        playerMovement.enabled = true;
        playerGun.gameObject.SetActive(true);
        Destroy(gunCopy.gameObject);

        //Sets the follow profile the the correct one based on the scene, and sets transitioning to false
        camFollow.ImportFollowProfile(SceneManager.GetActiveScene().name.Contains("Hub") ? "Hub Profile" : "Mission Profile");
        isTransitioning = false;
    }

    void OnDrawGizmos()
    {
        if(GameObject.Find("Deposit Box"))
        {
            depositBox = GameObject.Find("Deposit Box");
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(depositBox.transform.position + depositBoxPickupArea.center, depositBoxPickupArea.size);
        }
    }
}
