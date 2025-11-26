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
        Vector2 angle = Angle2D.GetAngle<Vector2>(Variables.player.Find("Gun").position, World.mousePos, -90f + Random.Range(-weapon.spread / 2f, weapon.spread / 2f));

        RaycastHit2D hit = Physics2D.Raycast(Variables.player.position, angle, weapon.maxHitscanDistance, ~LayerMask.GetMask("Player"));
        Particles.SpawnSquare(hit.point, size: .1f);

        if(hit) TriggerEffect(EffectTrigger.Hit, weapon);
        else TriggerEffect(EffectTrigger.TimeOut, weapon);
    }
}
