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
        TriggerEffect(EffectTrigger.Hit, shotBy, gameObject);
    }

    public static void Hitscan(WeaponStats weapon)
    {
        Vector3 startPos = Variables.player.Find("Gun").position;

        Vector3 angle = Angle2D.GetAngle<Vector2>(startPos, World.mousePos, -90f + Random.Range(-weapon.spread / 2f, weapon.spread / 2f));

        RaycastHit2D hit = Physics2D.Raycast(startPos, angle, weapon.maxHitscanDistance, ~LayerMask.GetMask("Player"));
        
        Vector3 endPos = hit ? hit.point : startPos + angle * weapon.maxHitscanDistance;
        Visuals.SpawnLine(new(){startPos, endPos}, Color.yellow, .05f, .1f);

        if(hit) 
        {
            TriggerEffect(EffectTrigger.Hit, weapon);
            if(hit.collider.GetComponent<EnemyBehaviour>()) hit.collider.GetComponent<EnemyBehaviour>().hp -= weapon.damage;
        }
        else TriggerEffect(EffectTrigger.TimeOut, weapon);
    }

    public static void HitscanEnemy(Vector3 pos, Vector3 target, WeaponStats weapon)
    {
        Vector3 angle = Angle2D.GetAngle<Vector2>(pos, target, -90f + Random.Range(-weapon.spread / 2f, weapon.spread / 2f));
        RaycastHit2D hit = Physics2D.Raycast(pos, angle, weapon.maxHitscanDistance, ~LayerMask.GetMask("Enemy"));

        Vector3 endPos = hit ? hit.point : pos + angle * weapon.maxHitscanDistance;
        Visuals.SpawnLine(new(){pos, endPos}, Color.yellow, .05f, .1f);
    }
}
