using UnityEngine;
using MilanUtils;
using System.Collections;
using System.Collections.Generic;
using static ModuleApplyHandler;

[RequireComponent(typeof(TrailRenderer), typeof(Rigidbody2D))]
public class BulletBehaviour : MonoBehaviour
{
    public WeaponStats shotBy;

    TrailRenderer trailRend;
    Rigidbody2D rb;

    readonly List<ItemInfo> items = new();
    readonly Dictionary<string, float> itemValues = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trailRend = GetComponent<TrailRenderer>(); 
        rb = GetComponent<Rigidbody2D>();
        
        if(!shotBy) return;

        foreach(var item in appliedItems[shotBy.name]) items.Add(allItems[item]);
        items.Sort((x, y) => {return x.effectOrder.CompareTo(y.effectOrder);});
        foreach(var item in items)
        {
            foreach(var itVal in item.itemValues.list)
            {
                itemValues.Add(itVal.Key, itVal.Value);
            }
        }
    }

    void OnEnable()
    {
        if(!trailRend) Start();
        StopCoroutine(nameof(DisableCoroutine));
    }

    void OnDisable()
    {
        if(!trailRend) Start();
        trailRend.Clear();
    }

    void LateUpdate()
    {
        if(trailRend.enabled == false) trailRend.enabled = true;

        foreach(var item in items)
        {
            switch (item.name)
            {
                default: continue; 
            }
        }
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        bool destroyBullet = true;
        foreach(var item in items)
        {
            switch (item.name)
            {
                case "Bouncy Bullets":
                    if(itemValues["Max Bounces"] > .5f)
                    {
                        destroyBullet = false;
                        rb.sharedMaterial = (PhysicsMaterial2D)Variables.resources["Bounce Material"];
                        itemValues["Max Bounces"]--;
                    }
                    break;
                default: continue; 
            }
        }

        if(coll.gameObject.CompareTag("Map") && destroyBullet)
        {
            gameObject.SetActive(false);
        }
        else if(coll.gameObject.CompareTag("Enemy"))
        {
            EnemyBehaviour enemyBehaviour = coll.gameObject.GetComponent<EnemyBehaviour>();
            if(enemyBehaviour) enemyBehaviour.hp -= shotBy.damage;
            FloatingText.SpawnDamageText(coll.gameObject, shotBy.damage);
            
            gameObject.SetActive(false);
        }
    }

    public void Disable(float afterSeconds){StartCoroutine(DisableCoroutine(afterSeconds));}
    IEnumerator DisableCoroutine(float afterSeconds){yield return Variables.GetWaitForSeconds(afterSeconds); gameObject.SetActive(false);}
}
