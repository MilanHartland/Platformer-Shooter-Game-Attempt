using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using System.Collections;
using System.Collections.Generic;
using static ModuleApplyHandler;
using UnityEngine.SceneManagement;
using System.Linq;

[RequireComponent(typeof(TrailRenderer), typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BulletBehaviour : MonoBehaviour
{
    WeaponStats shotBy;

    TrailRenderer trailRend;
    Rigidbody2D rb;

    List<ItemInfo> items = new();
    Dictionary<string, float> itemValues = new();
    public Dictionary<string, object> info = new();
    List<GameObject> allEnemies = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trailRend = GetComponent<TrailRenderer>(); 
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetShotBy(WeaponStats w)
    {
        shotBy = (WeaponStats)ScriptableObject.CreateInstance(nameof(WeaponStats));
        shotBy.SetValues(w);

        items = new();
        itemValues = new();
        //Gets the list of items by copying each item in appliedItems, sorts it by effectOrder, then gets all item values
        foreach(var item in appliedItems[shotBy.name]) items.Add(item);
        items.Sort((x, y) => {return x.effectOrder.CompareTo(y.effectOrder);});
        foreach(var item in items)
        {
            foreach(var itVal in item.itemValues.list)
            {
                itemValues.Add(itVal.Key, itVal.Value);
            }
        }

        //Resets info
        info = new()
        {
            {"Start Time", Time.time},
            {"Start Position", transform.position},
            {"Stored Position", transform.position},
            {"Velocity", rb.linearVelocity},
            {"Start Velocity", rb.linearVelocity},
            {"Stored Velocity", rb.linearVelocity},
        };

        allEnemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();
        SceneManager.activeSceneChanged += (_, _) => {allEnemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();};
    }

    void OnEnable()
    {
        if(!rb || !trailRend) Start();

        trailRend.enabled = true;
    }

    public void SetStartValues()
    {
        info["Start Time"] = Time.time;
        info["Start Position"] = transform.position;
        info["Start Velocity"] = rb.linearVelocity;
    }

    void OnDisable()
    {
        if(!rb || !trailRend) Start();

        trailRend.Clear();
        GetComponent<Collider2D>().isTrigger = false;

        //Resets info
        info = new()
        {
            {"Start Time", Time.time},
            {"Start Position", transform.position},
            {"Stored Position", transform.position},
            {"Velocity", rb.linearVelocity},
            {"Start Velocity", rb.linearVelocity},
            {"Stored Velocity", rb.linearVelocity},
        };
    }

    void FixedUpdate()
    {
        info["Velocity"] = rb.linearVelocity;

        foreach(var item in items)
        {
            switch (item.name)
            {
                case "Thrust":
                    rb.linearVelocity *= 1 + itemValues["Thrust"] * Time.fixedDeltaTime;
                    break;
                case "Flicker":
                    //If not a copy and hasn't copied yet, and more than delay time has passed: spawn bullet, set position/shotBy, set active, set linearVelocity, set isCopy/hasCopied
                    if(!info.ContainsKey("Is Copy") && !info.ContainsKey("Has Copied") && Time.time > (float)info["Start Time"] + itemValues["Flicker Delay"])
                    {
                        GameObject obj = PlayerManager.SpawnBullet();
                        obj.transform.position = (Vector3)info["Start Position"];
                        obj.SetActive(true); 
                        obj.GetComponent<BulletBehaviour>().SetShotBy(shotBy);
                        obj.GetComponent<Rigidbody2D>().linearVelocity = (Vector2)info["Start Velocity"];     
                        obj.GetComponent<BulletBehaviour>().info.Add("Is Copy", true);
                        info.Add("Has Copied", true);
                    }
                    break;
                case "Proximity Freeze":
                    //If can't freeze, break
                    if(info.ContainsKey("Unfreezable")) break;

                    //Sets 2 bools; 1 for if it should freeze right now, and 1 for if it was already frozen
                    bool freeze = false;
                    bool wasFrozen = info.ContainsKey("Frozen");

                    //Checks each enemy
                    for(int i = 0; i < allEnemies.Count; i++)
                    {
                        //If close enough to the enemy      
                        if(Vector2.Distance(transform.position, allEnemies[i].transform.position) < itemValues["Proximity Freeze Distance"])
                        {
                            freeze = true;

                            //If just added "Frozen" to info, set stored position to current position
                            if(info.TryAdd("Frozen", Time.time))
                                info["Stored Position"] = transform.position;
                            
                            //If it was already present and the current time is above the freeze start + length, unfreeze
                            else if(Time.time > (float)info["Frozen"] + itemValues["Proximity Freeze Length"]) freeze = false;
                            
                            //Zeros out velocity and sets position to the stored position
                            rb.linearVelocity = Vector3.zero;
                            transform.position = (Vector3)info["Stored Position"];
                        }
                    }

                    //If shouldn't freeze right now but was frozen, remove frozen from info and add unfreezable
                    if(!freeze && wasFrozen)
                    {
                        info.Remove("Frozen");
                        info.TryAdd("Unfreezable", true);
                    }
                    break;
                case "Grenadier":
                    //If the current time is above the start time + timer
                    if(Time.time > (float)info["Start Time"] + itemValues["Grenadier Timer"])
                    {
                        //Spawns an explosion particle system, and sets the size to 2x the radius
                        ParticleSystem ps = Effects.CreateTempParticleSystem(prefabs["Simple Explosion"], transform.position, Quaternion.identity);
                        ps.transform.localScale = 2f * itemValues["Grenadier Explosion Size"] * Vector3.one;

                        //Checks each enemy if the closest point of the hitbox is within explosion range, and deals damage accordingly
                        for(int i = 0; i < allEnemies.Count; i++)
                        {
                            if(Vector2.Distance(transform.position, allEnemies[i].GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Grenadier Explosion Size"])
                                DamageEnemy(allEnemies[i], itemValues["Grenadier Explosion Damage"] * shotBy.damage);
                        }

                        //If the player is in explosion range, damage the player for half of the normal damage
                        if(Vector2.Distance(transform.position, player.GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Grenadier Explosion Size"])
                            player.GetComponent<PlayerManager>().hp -= itemValues["Grenadier Explosion Size"] * shotBy.damage * .5f;

                        gameObject.SetActive(false);
                    }
                    break;
                case "Ether": //ChatGPT code because I was going to kill either myself or someone else
                    // cache components used here
                    var col = GetComponent<Collider2D>();
                    var circle = GetComponent<CircleCollider2D>();
                    if (circle == null) break; // safety

                    // layer mask for Map (uses the layer name "Map")
                    int mapMask = LayerMask.GetMask("Map");

                    // detect whether we're overlapping any Map collider right now
                    bool overlappingMap = Physics2D.OverlapCircle(transform.position, circle.radius * 1.1f, mapMask) != null;

                    // previous frame overlap state stored in info dictionary (defaults to false)
                    bool wasOverlapping = info.ContainsKey("WasOverlappingMap") && (bool)info["WasOverlappingMap"];

                    // ENTER: we started overlapping map this frame
                    if (overlappingMap && !wasOverlapping)
                    {
                        // only start ethering if we have ethers left
                        if (itemValues["Max Ethers"] > .5f && !info.ContainsKey("Is Ethered"))
                        {
                            info.TryAdd("Is Ethered", true);
                            col.isTrigger = true;
                        }
                    }
                    // EXIT: we stopped overlapping map this frame and we were ethered
                    else if (!overlappingMap && info.ContainsKey("Is Ethered"))
                    {
                        info.Remove("Is Ethered");
                        itemValues["Max Ethers"]--;
                        col.isTrigger = false;
                    }

                    // store current overlap state for next frame
                    info["WasOverlappingMap"] = overlappingMap;
                    break;
                case "Static Cloud":
                    //Checks each enemy if the closest point of the hitbox is within cloud range, and deals damage accordingly
                    for(int i = 0; i < allEnemies.Count; i++)
                    {
                        if(Vector2.Distance(transform.position, allEnemies[i].GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Static Cloud Size"])
                            DamageEnemy(allEnemies[i], itemValues["Static Cloud Damage"] * shotBy.damage);
                    }
                    break;
                default: continue; 
            }
        }
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        bool destroyBullet = true;
        bool stillColliding = true;
        Vector3 hitPoint = coll.GetContact(0).point;
        foreach(var item in items)
        {
            switch (item.name)
            {
                case "Bouncy Bullets":
                    //If touching something, has bounces left, and colliding with the map
                    if(stillColliding && itemValues["Max Bounces"] > .5f && coll.gameObject.CompareTag("Map"))
                    {
                        //Don't destroy bullet, set RigidBody material to bounce material, decrease max bounces, and set stillColliding to false
                        destroyBullet = false;
                        rb.linearVelocity = Vector2.Reflect((Vector2)info["Velocity"], coll.GetContact(0).normal);
                        itemValues["Max Bounces"]--;
                        stillColliding = false;
                    }
                    break;
                case "Explode":
                    //Spawns an explosion particle system, and sets the size to 2x the radius
                    ParticleSystem ps = Effects.CreateTempParticleSystem(prefabs["Simple Explosion"], hitPoint, Quaternion.identity);
                    ps.transform.localScale = 2f * itemValues["Explosion Size"] * Vector3.one;
                    
                    //Checks each enemy if the closest point of the hitbox is within explosion range, and deals damage accordingly
                    for(int i = 0; i < allEnemies.Count; i++)
                    {
                        if(Vector2.Distance(hitPoint, allEnemies[i].GetComponent<BoxCollider2D>().ClosestPoint(hitPoint)) < itemValues["Explosion Size"])
                            DamageEnemy(allEnemies[i],itemValues["Explosion Damage"] * shotBy.damage);
                    }

                    //If the player is in explosion range, damage the player for half of the normal damage
                    if(Vector2.Distance(transform.position, player.GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Grenadier Explosion Size"])
                            player.GetComponent<PlayerManager>().hp -= itemValues["Grenadier Explosion Size"] * shotBy.damage * .5f;
                    break;
                case "Grenadier":
                    //If still colliding and colliding with the map
                    if(stillColliding && coll.gameObject.CompareTag("Map"))
                    {
                        //Don't destroy bullet, set RigidBody material to bounce material, set bounciness and friction to the correct values, and set stillColliding to false
                        destroyBullet = false;
                        rb.linearVelocity = Vector2.Reflect((Vector2)info["Velocity"], coll.GetContact(0).normal) * itemValues["Grenadier Bounce Energy"];
                        stillColliding = false;
                    }
                    break;
                case "Shatter":
                    //If still colliding, colliding with the map, and isn't shattered
                    if(stillColliding && coll.gameObject.CompareTag("Map") && !info.ContainsKey("Is Shattered"))
                    {
                        //Makes a bullet for each shatter count
                        for(int i = 0; i < itemValues["Shatter Count"]; i++)
                        {
                            //Spawns a bullet and sets the position to this position, but slightly off from the wall (to not instantly have OnCollisionEnter2D trigger)
                            GameObject bullet = PlayerManager.SpawnBullet();
                            bullet.transform.position = transform.position + (Vector3)coll.GetContact(0).normal * .1f;

                            //Gets the BulletBehaviour, sets shotBy, sets damage, adds Is Shattered, and copies this item's itemValues (just for good measure)
                            BulletBehaviour bulBeh = bullet.GetComponent<BulletBehaviour>();
                            bulBeh.SetShotBy(shotBy);
                            bulBeh.shotBy.damage *= itemValues["Shatter Damage"];
                            bulBeh.info.Add("Is Shattered", true);
                            bulBeh.itemValues = itemValues;

                            //Enables the bullet
                            bullet.SetActive(true);

                            //Gets the bounce velocity (reflected), and sets the velocity to magnitude * normalized (((velocity vector) to float, add random offset) to vector) vector
                            Vector2 vel = Vector2.Reflect((Vector2)info["Velocity"], coll.GetContact(0).normal);
                            vel = vel.magnitude * Angle2D.Convert<float, Vector2>(Angle2D.Convert<Vector2, float>(vel) + Random.Range(-itemValues["Shatter Spread"], itemValues["Shatter Spread"])).normalized;
                            bullet.GetComponent<Rigidbody2D>().linearVelocity = vel;
                        }

                        //Kills this bullet
                        gameObject.SetActive(false);
                    }
                    break;
                default: continue; 
            }
        }

        if(coll.gameObject.CompareTag("Enemy"))
        {
            EnemyBehaviour enemyBehaviour = coll.gameObject.GetComponent<EnemyBehaviour>();
            
            foreach(var item in items)
            {
                switch (item.name)
                {
                    case "Infect":
                        //Apply infection to the enemy
                        enemyBehaviour.ApplyInfection(shotBy.damage / itemValues["Infection Damage"], itemValues["Infection Duration"]);
                        break;
                    case "Grenadier":
                            //Spawns an explosion particle system, and sets the size to 2x the radius
                        ParticleSystem ps = Effects.CreateTempParticleSystem(prefabs["Simple Explosion"], transform.position, Quaternion.identity);
                        ps.transform.localScale = 2f * itemValues["Grenadier Explosion Size"] * Vector3.one;

                        //Checks each enemy if the closest point of the hitbox is within explosion range, and deals damage accordingly
                        for(int i = 0; i < allEnemies.Count; i++)
                        {
                            if(Vector2.Distance(transform.position, allEnemies[i].GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Grenadier Explosion Size"])
                                DamageEnemy(allEnemies[i], itemValues["Grenadier Explosion Damage"] * shotBy.damage);
                        }

                        //If the player is in explosion range, damage the player for half of the normal damage
                        if(Vector2.Distance(transform.position, player.GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Grenadier Explosion Size"])
                        player.GetComponent<PlayerManager>().hp -= itemValues["Grenadier Explosion Size"] * shotBy.damage * .5f;
                        break;
                    case "Gamble":
                        shotBy.damage *= Random.Range(1f - itemValues["Gamble Strength"], 1f + itemValues["Gamble Strength"]);
                        break;
                    case "Cripple":
                        if(!enemyBehaviour.info.TryAdd("Cripple", itemValues["Cripple Strength"]))
                        {
                            enemyBehaviour.info["Cripple"] += itemValues["Cripple Strength"];
                        }
                        break;
                    default: continue;
                }
            }

            DamageEnemy(coll.gameObject, shotBy.damage);
        }

        if(destroyBullet) gameObject.SetActive(false);
    }

    void DamageEnemy(GameObject enemy, float damage)
    {
        enemy.GetComponent<EnemyBehaviour>().TakeDamage(damage);

        foreach(var item in items)
        {
            switch (item.name)
            {
                case "Chain":
                    allEnemies.Sort((x, y) => Vector2.Distance(transform.position, x.GetComponent<BoxCollider2D>().ClosestPoint(transform.position))
                    .CompareTo(Vector2.Distance(transform.position, y.GetComponent<BoxCollider2D>().ClosestPoint(transform.position))));

                    foreach(var obj in allEnemies)
                    {
                        if(obj != enemy && itemValues["Chain Count"] > .5 &&
                        Vector2.Distance(transform.position, obj.GetComponent<BoxCollider2D>().ClosestPoint(transform.position)) < itemValues["Chain Size"])
                        {
                            obj.GetComponent<EnemyBehaviour>().TakeDamage(damage * itemValues["Chain Damage"]);
                            Effects.SpawnLine(new(){enemy.transform.position, obj.transform.position}, Color.red, .1f, .1f).transform.position += Vector3.forward * 10f;
                            itemValues["Chain Count"]--;
                        }
                    }
                    break;
                default: continue;
            }
        }
    }
}