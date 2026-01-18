using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(PlayerMovement)), RequireComponent(typeof(AnimationManager), typeof(Rigidbody2D))]
public class PlayerManager : MonoBehaviour
{
    public static WeaponStats mainWeapon => WeaponInfo.GetWeaponWithModifiers(baseMainWeapon);
    public static WeaponStats altWeapon => WeaponInfo.GetWeaponWithModifiers(baseAltWeapon);
    public static WeaponStats baseMainWeapon, baseAltWeapon, heldWeapon;
    public static Timer heldWeaponTimer;
    public static int mainWeaponBullets, altWeaponBullets, heldWeaponBullets;
    public static bool isReloading;

    public static List<GameObject> equippedItems = new();

    public float maxHP;
    public float hp {get; set;}
    public static bool isDead;

    public static int oreCount;

    public static Queue<GameObject> bulletPool = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += SetEquippedItems;
        DragDrop.dragOutAction += SetEquippedItems;
        DragDrop.dragInAction += ReEquipWeapon;
        DragDrop.dragOutAction += ReEquipWeapon;

        hp = maxHP;
        isDead = false;

        FillBulletPool(100);

        SceneManager.activeSceneChanged += DisableAllBullets;
    }

    private void DisableAllBullets(Scene arg0, Scene arg1){foreach(var obj in bulletPool){obj.SetActive(false);}}

    void FillBulletPool(int count)
    {
        Transform bulletParent = GameObject.Find("Bullet Parent").transform;
        for(int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefabs["Bullet"]);
            obj.transform.SetParent(bulletParent);
            obj.SetActive(false);
            bulletPool.Enqueue(obj);
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.Find("Gun").gameObject.SetActive(heldWeapon != null);

        // GetComponent<AnimationManager>().SetArmAngle(-45f);
        if(Input.GetKeyDown(KeyCode.Z)) SaveSystem.SaveToFile();
        if(Input.GetKeyDown(KeyCode.X)) SaveSystem.Load();
        
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
            StartCoroutine(ReloadCoroutine());

        //If heldweapon, has bullets, and pressing shoot or holding shoot with automatic, and shooting cooldown over, decrease bullets, reset timer, and fire weapon
        if(heldWeapon != null && (Input.GetKeyDown(Keybinds.shoot) || Input.GetKey(Keybinds.shoot) && heldWeapon.automatic) && heldWeaponTimer)
        {
            if(heldWeaponBullets > 0)
            {
                heldWeaponTimer.ResetTimer();

                if(heldWeapon == mainWeapon) mainWeaponBullets--;
                else altWeaponBullets--;

                heldWeaponBullets--;
                FireWeapon(heldWeapon);
            }
            else StartCoroutine(ReloadCoroutine());
        }
    }

    void ReEquipWeapon(DragDrop a, DragDrop b)
    {
        if(heldWeapon == null) return;
        else if(heldWeapon == mainWeapon) EquipWeapon(mainWeapon);
        else if(heldWeapon == altWeapon) EquipWeapon(altWeapon);
    }
    
    //Sets heldweapon and heldweapontimer. If it's mainweapon, set weaponbullets to mainweaponbullets, otherwise altweaponbullets. Then, reset and start weapon cooldown timer
    void EquipWeapon(WeaponStats weap)
    {
        transform.Find("Gun").GetComponent<SpriteRenderer>().sprite = prefabs[weap.name].GetComponent<WeaponInfo>().weaponSprite;
        
        heldWeapon = weap;
        heldWeaponTimer = new(1f / weap.fireRate);
        
        if(weap == mainWeapon)
            heldWeaponBullets = mainWeaponBullets;
        else heldWeaponBullets = altWeaponBullets;

        heldWeaponTimer.ResetTimer();
        heldWeaponTimer.StartTimer();
    }
    
    IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        //Set a LeanTween rotate for the gun object. For the duration of the reload, set AnimationManager ArmAngle to the angle of the Tween
        AnimationManager animManager = GetComponent<AnimationManager>();
        float timeStart = Time.time;
        LeanTween.rotateZ(transform.Find("Gun").gameObject, Mathf.Sign(transform.localScale.x) * 160f, heldWeapon.reloadTime).setEaseOutCubic();
        while(Time.time < timeStart + heldWeapon.reloadTime)
        {
            animManager.SetArmAngle(-transform.Find("Gun").eulerAngles.z);
            yield return null;
        }

        if(heldWeapon == mainWeapon) mainWeaponBullets = mainWeapon.magazineSize;
        else if(heldWeapon == altWeapon) altWeaponBullets = altWeapon.magazineSize;

        heldWeaponBullets = heldWeapon.magazineSize;

        isReloading = false;
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
                GameObject itemObj = World.FindInactive(item.name);
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
        
        //Sets dead to true and deletes equipped items, as well as turning the player to particles
        isDead = true;
        DeleteEquippedItems();
        Effects.TurnIntoParticles(gameObject, transform.position, dontThrowNonReadException: true);

        //Creates a scene loading variable, and makes it not automatically switch scenes
        var sceneLoading = SceneManager.LoadSceneAsync(1);
        sceneLoading.allowSceneActivation = false;

        //Waits until the respawn bind is pressed and the scene is done loading
        bool pressedRespawn = false;
        while (sceneLoading.progress < .9f || !pressedRespawn) 
        {
            if(Input.GetKeyDown(Keybinds.bindings["Interact"])) 
                pressedRespawn = true; 
            yield return null;
        }

        //Moves the player out of the way and disables camera follow
        player.position = Vector3.one * 9999f;
        Camera.main.GetComponent<CameraFollow>().enabled = false;

        //Moves the camera upwards for camFlyHeight units over the course of camFlyTime seconds, with ease in/out, and waits for that time
        LeanTween.moveY(Camera.main.gameObject, Camera.main.transform.position.y + transVars.camFlyHeight, transVars.camFlyTime).setEaseInQuad();
        yield return GetWaitForSeconds(transVars.camFlyTime);

        //Resets HP, loads scene, sets camera position to respawn enter point, sets player position to respawn point, and sets deposit box position to teleport point
        hp = maxHP;
        sceneLoading.allowSceneActivation = true;
        yield return sceneLoading;
        Camera.main.transform.position = GameObject.Find("Camera Respawn Enter Point").transform.position;
        player.position = GameObject.Find("Player Respawn Point").transform.position;
        GameObject.Find("Deposit Box").transform.position = GameObject.Find("Deposit Box Teleport Point").transform.position;

        //Reenables all player things
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;
        GetComponent<PlayerMovement>().enabled = true;
        GetComponent<AnimationManager>().enabled = true;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        foreach(Transform child in transform){child.GetComponent<SpriteRenderer>().enabled = true;}

        isDead = false; //Sets dead to false

        //Moves the camera to the stop point with ease in/out, and waits for that time
        LeanTween.move(Camera.main.gameObject, GameObject.Find("Camera Respawn Stop Point").transform.position, transVars.camStopTime).setEaseOutQuad();
        yield return GetWaitForSeconds(transVars.camStopTime);

        //Reenables camera follow and sets the profile
        Camera.main.GetComponent<CameraFollow>().enabled = true;
        Camera.main.GetComponent<CameraFollow>().ImportFollowProfile("Hub Profile");
    }

    void FireWeapon(WeaponStats w)
    {
        if (w == null) return; //Checks if there is a weapon to fire

        //Creates a muzzle flash particle system that gets deleted when it finishes
        Quaternion rot = Quaternion.Euler(transform.Find("Gun").eulerAngles + Vector3.forward * -90f);
        Effects.CreateTempParticleSystem(prefabs["Simple Muzzle Flash"], player.transform.Find("Gun").position, rot);

        //Gets the amount of bullets shot, which is the floor of the count (example: 3.4 -> 3), and if the random chance of the remainder is hit (3.4 -> 40%), add 1
        int bulletCount = Mathf.FloorToInt(w.bulletCount);
        if(Random.Range(0f, 1f) <= w.bulletCount - Mathf.Floor(w.bulletCount)) bulletCount++;

        //For each bullet to fire, spawn it from pool, set position/rotation, set active, set velocity/shotBy, and run SetStartValues (can't go in OnEnable because for velocity object needs to be active)
        for(int i = 0; i < bulletCount; i++)
        {
            GameObject obj = SpawnBullet();
            obj.transform.position = player.transform.Find("Gun").position;
            Angle2D.TurnTo(obj, World.mousePos, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
            obj.SetActive(true);
            obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.up * w.bulletSpeed;
            obj.GetComponent<BulletBehaviour>().SetShotBy(w);
            obj.GetComponent<BulletBehaviour>().SetStartValues();
        }
    }

    /// <summary>Spawns a bullet from the bullet pool. Needs to be manually set active. Does not set any BulletBehaviour variables</summary><returns></returns>
    public static GameObject SpawnBullet()
    {
        GameObject obj = bulletPool.Dequeue();
        obj.GetComponent<TrailRenderer>().Clear();
        obj.GetComponent<TrailRenderer>().enabled = false;
        bulletPool.Enqueue(obj);
        return obj;
    }
}
