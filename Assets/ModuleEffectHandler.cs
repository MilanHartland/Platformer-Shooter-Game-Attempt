using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class ModuleEffectHandler : MonoBehaviour
{
    public static WeaponStats weapon, altWeapon;
    public static Timer weaponTimer, altWeaponTimer;

    public static Dictionary<string, List<string>> appliedItems = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += ApplyModule;
        DragDrop.dragOutAction += ApplyModule;

        TryAutoSetValues();
        LoadAllResources();

        appliedItems = new();
        foreach(var obj in resources)
        {
            if(obj.Value.GetType() == typeof(WeaponStats))
                appliedItems.Add(((WeaponStats)obj.Value).name, new());
        }
     }

    // Update is called once per frame
    void Update()
    {
        if(MenuManager.IsPaused) return;

        if(Input.GetKeyDown(KeyCode.G))
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (Input.GetKeyDown(KeyCode.T)) Visuals.Disintegrate(player.gameObject, dontThrowNonReadException: true);

        if (weapon != null && (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && weapon.automatic)) && weaponTimer)
        {
            weaponTimer.ResetTimer();
            FireWeapon(weapon);
        }

        if (altWeapon != null && (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && altWeapon.automatic)) && altWeaponTimer)
        {
            altWeaponTimer.ResetTimer();
            FireWeapon(altWeapon);
        }
    }
    
    void FireWeapon(WeaponStats w)
    {
        if (w == null) return;

        Visuals.CreateTempParticleSystem(prefabs["Simple Muzzle Flash"], player.transform.Find("Gun").position, player.transform.Find("Gun").rotation);

        if(w.firingType == WeaponStats.FiringType.Hitscan)
        {
            ProjectileHandler.Hitscan(w);
            return;
        }

        GameObject obj = Instantiate(prefabs["Bullet"]);
        obj.transform.position = player.transform.Find("Gun").position;
        Angle2D.TurnTo(obj, World.mousePos, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
        obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.up * w.bulletSpeed;
        obj.GetComponent<ProjectileHandler>().shotBy = w;
        Destroy(obj, 5f);
    }

    void ApplyModule(DragDrop item, DragDrop slot)
    {
        bool isSlotted = DragDrop.slottedItems.Contains(item);
        InfoTag tag = item.GetComponent<InfoTag>();

        if (item.name == "Weapon")
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
        else if(item.name == "Alt Weapon")
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
        else if (item.name == "Bullet" || item.name == "Effect")
        {
            //This needs explanation because of hierarchy in the inspector, which is as follows: Weapon Inventory Parent > (Background Panel > (Slots > (Items)), Weapon)
            //If is currently in a slot (which is in Background Panel, which is in Inventory Panel), add this to the appliedItems of the 2nd parent (Inventory Parent)'s name without " Inventory Parent"
            //Otherwise, if the new "slot" does contain slot in the name (so isn't a parent), remove it from appliedItems of the 2nd parent
            if (isSlotted)
                appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Add(tag.name);
            else if(slot.transform.name.Contains("Slot"))
                appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Remove(tag.name);
        }
    }

    public enum EffectTrigger{Fire, Hit, TimeOut, PhysicsFrame}
    public static void TriggerEffect(EffectTrigger trigger, WeaponStats weap, GameObject projectile = null)
    {
        foreach (string item in appliedItems[weap.name])
        {
            InfoTag tag = World.GameObjectWhere(x => {return x.GetComponent<InfoTag>() && x.GetComponent<InfoTag>().name == item;}, true).GetComponent<InfoTag>();
            Rigidbody2D rb = null;
            if(projectile) rb = projectile.GetComponent<Rigidbody2D>();

            if (trigger == EffectTrigger.Fire)
            {
                switch (item)
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
                switch (item)
                {
                    default:
                        break;
                }
            }
            else if (trigger == EffectTrigger.TimeOut)
            {
                switch (item)
                {
                    default:
                        break;
                }
            }
            else if (trigger == EffectTrigger.PhysicsFrame)
            {
                switch (item)
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
