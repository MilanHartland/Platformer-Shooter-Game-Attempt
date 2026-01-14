using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static WeaponStats mainWeapon => GetWeaponWithModifiers(baseMainWeapon);
    public static WeaponStats altWeapon => GetWeaponWithModifiers(baseAltWeapon);
    public static WeaponStats baseMainWeapon, baseAltWeapon, heldWeapon;
    public static Timer heldWeaponTimer;
    public static int mainWeaponBullets, altWeaponBullets, heldWeaponBullets;
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
        // if(Input.GetKeyDown(KeyCode.Z)) SaveSystem.SaveToFile();
        // if(Input.GetKeyDown(KeyCode.X)) SaveSystem.Load();
        
        if(MissionManager.inHub) hp = maxHP;
        if(MenuManager.IsPaused || (MissionManager.inHub && !HubManager.inShootingArea) || isDead || isReloading) return;

        //If no heldweapon, or heldweapon is not main/alt weapon, equip existing main/alt weapon
        if(heldWeapon == null || (heldWeapon != mainWeapon && heldWeapon != altWeapon))
        {
            if(mainWeapon != null) EquipWeapon(mainWeapon);
            else if(altWeapon != null) EquipWeapon(altWeapon);
        }

        //If pressing equip and not reloading, if possible, switch guns
        if (Input.GetKeyDown(Keybinds.equip) && !isReloading)
        {
            if(heldWeapon == mainWeapon && altWeapon != null) 
                EquipWeapon(altWeapon);
            else if(heldWeapon == altWeapon && mainWeapon != null)
                EquipWeapon(mainWeapon);
        }

        //If dead, start respawn coroutine
        if(hp <= 0f)
            StartCoroutine(RespawnCoroutine());

        //If pressing reload, reload
        if (Input.GetKeyDown(Keybinds.reload) && !isReloading && heldWeapon != null)
            ReloadInvoke();

        //If heldweapon, has bullets, and pressing shoot or holding shoot with automatic, and shooting cooldown over, decrease bullets, reset timer, and fire weapon
        if(heldWeapon != null && heldWeaponBullets > 0 && (Input.GetKeyDown(Keybinds.shoot) || Input.GetKey(Keybinds.shoot) && heldWeapon.automatic) && heldWeaponTimer)
        {
            heldWeaponTimer.ResetTimer();

            if(heldWeapon == mainWeapon) mainWeaponBullets--;
            else altWeaponBullets--;

            heldWeaponBullets--;
            FireWeapon(heldWeapon);
        }
    }
    
    //Sets heldweapon and heldweapontimer. If it's mainweapon, set weaponbullets to mainweaponbullets, otherwise altweaponbullets. Then, reset and start weapon cooldown timer
    void EquipWeapon(WeaponStats weap)
    {        
        heldWeapon = weap;
        heldWeaponTimer = new(1f / weap.fireRate);
        
        if(weap == mainWeapon)
            heldWeaponBullets = mainWeaponBullets;
        else heldWeaponBullets = altWeaponBullets;

        heldWeaponTimer.ResetTimer();
        heldWeaponTimer.StartTimer();
    }

    public static WeaponStats GetWeaponWithModifiers(WeaponStats weap)
    {
        if(weap == null) return null;
        
        WeaponStats w = ScriptableObject.CreateInstance<WeaponStats>();
        w.SetValues(weap);
        foreach(var item in ModuleApplyHandler.appliedItems[weap.name])
        {
            w = w.AddModifiers(ModuleApplyHandler.allModifiers[item]);
        }
        return w;
    }
    
    void ReloadInvoke(){isReloading = true; Invoke(nameof(Reload), heldWeapon.reloadTime);}
    void Reload()
    {
        isReloading = false;

        if(heldWeapon == mainWeapon) mainWeaponBullets = mainWeapon.magazineSize;
        else if(heldWeapon == altWeapon) altWeaponBullets = altWeapon.magazineSize;

        heldWeaponBullets = heldWeapon.magazineSize;
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
            baseMainWeapon = null;
            baseAltWeapon = null;
            heldWeapon = null;
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

        var sceneLoading = SceneManager.LoadSceneAsync(1);
        sceneLoading.allowSceneActivation = false;

        bool pressedRespawn = false;
        while (sceneLoading.progress < .9f || !pressedRespawn) 
        {
            if(Input.GetKeyDown(Keybinds.bindings["Interact"])) 
                pressedRespawn = true; 
            yield return null;
        }

        //TODO - ADD CODE SO THAT CAMERA FLIES UP, THEN SWITCHES SCENE, THEN MOVES TO CORRECT POSITION FOR RESPAWNED PLAYER
        player.position = Vector3.one * 9999f;
        Camera.main.GetComponent<CameraFollow>().enabled = false;

        LeanTween.moveY(Camera.main.gameObject, Camera.main.transform.position.y + transVars.camFlyHeight, transVars.camFlyTime).setEaseInQuad();
        yield return GetWaitForSeconds(transVars.camFlyTime);

        hp = maxHP;
        sceneLoading.allowSceneActivation = true;
        yield return sceneLoading;
        Camera.main.transform.position = GameObject.Find("Camera Respawn Enter Point").transform.position;
        player.position = GameObject.Find("Player Respawn Point").transform.position;
        GameObject.Find("Deposit Box").transform.position = GameObject.Find("Deposit Box Teleport Point").transform.position;

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
    }

    void FireWeapon(WeaponStats w)
    {
        if (w == null) return;

        Effects.CreateTempParticleSystem(prefabs["Simple Muzzle Flash"], player.transform.Find("Gun").position, player.transform.Find("Gun").rotation);

        int bulletCount = Mathf.FloorToInt(w.bulletCount);
        if(Random.Range(0f, 1f) <= w.bulletCount - Mathf.Floor(w.bulletCount)) bulletCount++;
        print($"{w.bulletCount} {w.bulletCount - Mathf.Floor(w.bulletCount)} {bulletCount}");
        for(int i = 0; i < bulletCount; i++)
        {
            GameObject obj = bulletPool.Dequeue();
            obj.SetActive(true);
            obj.GetComponent<TrailRenderer>().Clear();
            obj.GetComponent<TrailRenderer>().enabled = false;
            obj.transform.position = player.transform.Find("Gun").position;
            Angle2D.TurnTo(obj, World.mousePos, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
            obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.up * w.bulletSpeed;
            obj.GetComponent<BulletBehaviour>().shotBy = w;
            obj.GetComponent<BulletBehaviour>().Disable(5f);
            bulletPool.Enqueue(obj);
        }
    }
}
