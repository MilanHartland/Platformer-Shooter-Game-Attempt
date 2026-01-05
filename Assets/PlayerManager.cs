using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static WeaponStats mainWeapon, altWeapon;
    public static Timer mainWeaponTimer, altWeaponTimer;
    public static int mainWeaponBullets, altWeaponBullets;
    public static bool isReloading;

    public static List<GameObject> equippedItems = new();

    public float maxHP;
    public float hp {get; set;}
    public static bool isDead;

    Queue<GameObject> bulletPool = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += SetEquippedItems;
        DragDrop.dragOutAction += SetEquippedItems;

        hp = maxHP;
        isDead = false;

        StartCoroutine(FillBulletPool(100));

        SceneManager.activeSceneChanged += DisableAllBullets;

        Keybinds.bindings.Add("Respawn", Keybinds.interact);
    }

    private void DisableAllBullets(Scene arg0, Scene arg1){foreach(var obj in bulletPool){obj.SetActive(false);}}

    IEnumerator FillBulletPool(int count)
    {
        Transform bulletParent = GameObject.Find("Bullet Parent").transform;
        for(int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefabs["Bullet"]);
            obj.transform.SetParent(bulletParent);
            obj.SetActive(false);
            bulletPool.Enqueue(obj);
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Z)) SaveSystem.SaveToFile();
        if(Input.GetKeyDown(KeyCode.X)) SaveSystem.Load();
        
        if(MenuManager.IsPaused || (MissionManager.inHub && !HubManager.inShootingArea) || isDead || isReloading) return;

        if(hp <= 0f)
        {
            StartCoroutine(RespawnCoroutine());
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if(mainWeaponBullets <= 0 && mainWeapon != null) Reload(mainWeapon);
            else if(altWeaponBullets <= 0 && altWeapon != null) Reload(altWeapon);
        }
        
        if (mainWeapon != null && mainWeaponBullets > 0 && (Input.GetKeyDown(Keybinds.shoot) || (Input.GetKey(Keybinds.shoot) && mainWeapon.automatic)) && mainWeaponTimer)
        {
            mainWeaponTimer.ResetTimer();
            mainWeaponBullets--;
            FireWeapon(mainWeapon);
        }

        if (altWeapon != null && altWeaponBullets > 0 && (Input.GetKeyDown(Keybinds.alt) || (Input.GetKey(Keybinds.alt) && altWeapon.automatic)) && altWeaponTimer)
        {
            altWeaponTimer.ResetTimer();
            altWeaponBullets--;
            FireWeapon(altWeapon);
        }
    }
    
    void Reload(WeaponStats w){StartCoroutine(ReloadCouroutine(w));}
    IEnumerator ReloadCouroutine(WeaponStats w)
    {
        isReloading = true;
        yield return GetWaitForSeconds(w.reloadTime);
        isReloading = false;
        if(w == mainWeapon) mainWeaponBullets = mainWeapon.magazineSize;
        else if(w == altWeapon) altWeaponBullets = altWeapon.magazineSize;
    }

    //Sets all equipped items to the list
    void SetEquippedItems(DragDrop a, DragDrop b)
    {
        //Clears the list, then for each weapon, try to get the object and its copy (because spawner). If no copy or slottedItems does not contain the copy, continue.
        //Add the normal weapon to equippedItems, then for each item equipped to it, get the object. If it is null, throw an error, otherwise, add it to equippedItems
        equippedItems.Clear();
        foreach(var pair in ModuleApplyHandler.appliedItems)
        {
            GameObject keyObj = World.FindInactive(pair.Key);
            GameObject keyCopyObj = World.FindInactive(pair.Key + " Copy");
            if(!keyCopyObj || !DragDrop.slottedItems.Contains(keyCopyObj.GetComponent<DragDrop>())) continue;

            if(keyObj == null) Debug.LogError($"No weapon with name {pair.Key} found!");

            equippedItems.Add(keyObj);
            foreach(var item in pair.Value)
            {
                GameObject itemObj = World.FindInactive(item);
                if(itemObj == null) Debug.LogError($"No item with name {item} found!");
                equippedItems.Add(itemObj);
            }
        }
    }

    //Deletes the equipped items
    void DeleteEquippedItems()
    {
        //For each equipped items, get it and try get its copy (for if it is a weapon, which is a spawner). If there is a copy, unslot and destroy. Then, unslot, destroy, remove and null weapons
        for(int i = equippedItems.Count - 1; i >= 0; i--)
        {
            GameObject obj = equippedItems[i];
            GameObject copy = World.FindInactive(obj.name + " Copy");

            if(copy) 
            {
                DragDrop.UnSlot(copy);
                Destroy(copy);
            }
            
            DragDrop.UnSlot(obj);
            Destroy(obj.transform.parent.gameObject);
            ModuleApplyHandler.appliedItems.Remove(obj.name);
            mainWeapon = null;
            altWeapon = null;
        }
        equippedItems.Clear();
    }

    IEnumerator RespawnCoroutine()
    {
        //Gets transition variables and disables most things of player
        MissionManager.TransitionVariables transVars = gameManager.GetComponent<MissionManager>().transition;
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<AnimationManager>().enabled = false;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        foreach(Transform child in transform){child.GetComponent<SpriteRenderer>().enabled = false;}
        
        isDead = true;
        DeleteEquippedItems();
        Effects.TurnIntoParticles(gameObject, transform.position, dontThrowNonReadException: true);
        var projList = World.AllGameObjectsWhere(x => {return x.gameObject.layer == LayerMask.NameToLayer("Projectile");});

        for(int i = projList.Count - 1; i >= 0; i--){Destroy(projList[i]);}

        var sceneLoading = SceneManager.LoadSceneAsync(1);
        sceneLoading.allowSceneActivation = false;

        bool pressedRespawn = false;
        while (sceneLoading.progress < .9f || !pressedRespawn) 
        {
            if(Input.GetKeyDown(Keybinds.bindings["Respawn"])) 
                pressedRespawn = true; 
            yield return null;
        }

        //TODO - ADD CODE SO THAT CAMERA FLIES UP, THEN SWITCHES SCENE, THEN MOVES TO CORRECT POSITION FOR RESPAWNED PLAYER
        player.position = Vector3.one * 9999f;
        Camera.main.GetComponent<CameraFollow>().enabled = false;

        LeanTween.moveY(Camera.main.gameObject, Camera.main.transform.position.y + transVars.camFlyHeight, transVars.camFlyTime).setEaseInQuad();
        yield return GetWaitForSeconds(transVars.camFlyTime);

        sceneLoading.allowSceneActivation = true;
        yield return sceneLoading;
        Camera.main.transform.position = GameObject.Find("Camera Respawn Enter Point").transform.position;
        player.position = GameObject.Find("Player Respawn Point").transform.position;

        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;
        GetComponent<PlayerMovement>().enabled = true;
        GetComponent<AnimationManager>().enabled = true;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        foreach(Transform child in transform){child.GetComponent<SpriteRenderer>().enabled = true;}

        isDead = false;

        LeanTween.move(Camera.main.gameObject, GameObject.Find("Camera Respawn Stop Point").transform.position, transVars.camStopTime).setEaseOutQuad();
        yield return GetWaitForSeconds(transVars.camStopTime);

        Camera.main.GetComponent<CameraFollow>().enabled = true;
        Camera.main.GetComponent<CameraFollow>().ImportFollowProfile("Hub Profile");
        hp = maxHP;
    }

    void FireWeapon(WeaponStats w)
    {
        if (w == null) return;

        Effects.CreateTempParticleSystem(prefabs["Simple Muzzle Flash"], player.transform.Find("Gun").position, player.transform.Find("Gun").rotation);

        GameObject obj = bulletPool.Dequeue();
        obj.SetActive(true);
        obj.transform.position = player.transform.Find("Gun").position;
        Angle2D.TurnTo(obj, World.mousePos, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
        obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.up * w.bulletSpeed;
        obj.GetComponent<BulletBehaviour>().shotBy = w;
        obj.GetComponent<BulletBehaviour>().Disable(5f);
        bulletPool.Enqueue(obj);
    }
}
