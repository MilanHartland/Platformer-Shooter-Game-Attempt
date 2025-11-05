using MilanUtils;
using static MilanUtils.Objects;
using UnityEngine;

public class ModuleEffectHandler : MonoBehaviour
{
    InfoTag weaponTag, altWeaponTag;
    WeaponStats weapon, altWeapon;

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

        if (Input.GetMouseButtonDown(0))
        {
            FireWeapon(weapon);
        }

        if (Input.GetMouseButton(0) && weapon.automatic)
        {
            FireWeapon(weapon);
        }
    }
    
    void FireWeapon(WeaponStats w)
    {
        if (w == null) return;

        GameObject obj = Instantiate(prefabs["Bullet"]);
        float angle = NewAngle2D.GetAngle(obj.transform.position, World.mousePos);
        obj.transform.eulerAngles = new Vector3(0f, 0f, angle);
        obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.up * w.bulletSpeed;
        obj.transform.position = player.position;
        Destroy(obj, 5f);
    }

    void ApplyModule(DragDrop obj, DragDrop slot)
    {
        bool isSlotted = DragDrop.allSlots.Contains(obj);
        InfoTag tag = obj?.GetComponent<InfoTag>();

        if (obj.name == "Weapon")
        {
            weaponTag = tag;

            if (tag)
                weapon = (WeaponStats)resources[tag.name];
        }
    }
}
