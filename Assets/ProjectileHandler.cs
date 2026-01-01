using MilanUtils;
using static ModuleEffectHandler;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class ProjectileHandler : MonoBehaviour
{
    public WeaponStats shotBy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TriggerEffect(EffectTrigger.Fire, shotBy, gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TriggerEffect(EffectTrigger.PhysicsFrame, shotBy, gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.GetContact(0).collider.TryGetComponent(out EnemyBehaviour eb)) 
            eb.hp -= shotBy.damage;

        TriggerEffect(EffectTrigger.Hit, shotBy, gameObject);
    }

    public static void Hitscan(WeaponStats w)
    {
        Vector3 startPos = Variables.player.Find("Gun").position;

        Vector3 angle = Angle2D.GetAngle<Vector2>(startPos, World.mousePos, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));

        RaycastHit2D hit = Physics2D.Raycast(startPos, angle, w.maxHitscanDistance, ~LayerMask.GetMask("Player"));
        
        Vector3 endPos = hit ? hit.point : startPos + angle * w.maxHitscanDistance;
        Effects.SpawnLine(new(){startPos, endPos}, Color.yellow, .05f, .1f);

        if(hit) 
        {
            TriggerEffect(EffectTrigger.Hit, w);
            if(hit.collider.GetComponent<EnemyBehaviour>()) hit.collider.GetComponent<EnemyBehaviour>().hp -= w.damage;
        }
        else TriggerEffect(EffectTrigger.TimeOut, w);
    }

    public static void HitscanEnemy(Vector3 pos, Vector3 target, WeaponStats w)
    {
        Vector3 angle = Angle2D.GetAngle<Vector2>(pos, target, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
        RaycastHit2D hit = Physics2D.Raycast(pos, angle, w.maxHitscanDistance, ~LayerMask.GetMask("Enemy"));

        Vector3 endPos = hit ? hit.point : pos + angle * w.maxHitscanDistance;
        Effects.SpawnLine(new(){pos, endPos}, Color.yellow, .05f, .1f);

        if(hit.collider.TryGetComponent(out PlayerManager pm)) pm.hp -= w.damage;
    }
}
