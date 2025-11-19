using MilanUtils;
using static MilanUtils.Objects;
using UnityEngine;
using System.Collections.Generic;

public class ModuleEffectHandler : MonoBehaviour
{
    public static WeaponStats weapon, altWeapon;
    public static Timer weaponTimer, altWeaponTimer;

    [HideInInspector] public List<string> appliedItems;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += ApplyModule;
        DragDrop.dragOutAction += ApplyModule;

        TryAutoSetValues();
        LoadAllResources();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameObject canvas = World.GameObjectWhere(x => { return x.name == "Canvas"; }, true);
            canvas.SetActive(!canvas.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.T)) Disintegrate(player);

        if (!weapon) return;

        if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && weapon.automatic))
        {
            FireWeapon(weapon);
        }

        if (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && altWeapon.automatic))
        {
            FireWeapon(altWeapon);
        }
    }
    
    void FireWeapon(WeaponStats w)
    {
        if (w == null || !weaponTimer) return;
        weaponTimer.ResetTimer();

        GameObject obj = Instantiate(prefabs["Bullet"]);
        obj.transform.position = player.transform.Find("Gun").position;
        Angle2D.TurnTo(obj, World.mousePos, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
        obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.up * w.bulletSpeed;
        Destroy(obj, 5f);
    }

    void ApplyModule(DragDrop obj, DragDrop slot)
    {
        bool isSlotted = DragDrop.slottedItems.Contains(obj);
        InfoTag tag = obj.GetComponent<InfoTag>();

        if (obj.name == "Weapon")
        {
            if (isSlotted)
            {
                weapon = (WeaponStats)resources[tag.name];
                weaponTimer = new(1f / weapon.fireRate);
            }
            else
            {
                weapon = null;
            }
        }
        else if(obj.name == "Alt Weapon")
        {
            if (isSlotted)
            {
                altWeapon = (WeaponStats)resources[tag.name];
                altWeaponTimer = new(1f / altWeapon.fireRate);
            }
            else
            {
                altWeapon = null;
            }
        }
        else if (obj.name == "Bullet" || obj.name == "Effect")
        {
            if (isSlotted)
                appliedItems.Add(tag.name);
            else
                appliedItems.Remove(tag.name);
        }
    }

    public enum EffectTrigger{Fire, Hit, TimeOut, PhysicsFrame}
    public static void TriggerEffect(EffectTrigger trigger, GameObject projectile, Collision2D coll = null)
    {
        foreach (DragDrop dd in DragDrop.slottedItems)
        {
            if (dd.name != "Bullet" && dd.name != "Effect") continue;

            InfoTag tag = dd.GetComponent<InfoTag>();
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

            if (trigger == EffectTrigger.Fire)
            {
                switch (tag.name)
                {
                    case "Reverse Gravity":
                        rb.gravityScale = -1f;
                        break;
                    case "Bounce":
                        rb.sharedMaterial = (PhysicsMaterial2D)resources["Bounce Material"];
                        break;
                    default:
                        break;
                }
            }
            else if (trigger == EffectTrigger.Hit)
            {
                switch (tag.name)
                {
                    default:
                        break;
                }
            }
            else if (trigger == EffectTrigger.TimeOut)
            {
                switch (tag.name)
                {
                    default:
                        break;
                }
            }
            else if (trigger == EffectTrigger.PhysicsFrame)
            {
                switch (tag.name)
                {
                    case "Exponential Speed":
                        rb.linearVelocity = (rb.linearVelocity.magnitude + tag.value) * rb.linearVelocity.normalized;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
