using MilanUtils;
using UnityEngine;

public class ModuleEffectHandler : MonoBehaviour
{
    InfoTag weapon, altWeapon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += ApplyModule;
        DragDrop.dragOutAction += ApplyModule;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameObject canvas = World.GameObjectWhere(x => { return x.name == "Canvas"; }, true);
            canvas.SetActive(!canvas.activeSelf);
        }

        if (Input.GetMouseButtonDown(0) && weapon != null) GameObject.Find("Game Manager").GetComponent<WeaponModules>().FireWeapon(weapon);
        if (Input.GetMouseButtonDown(1) && altWeapon != null) GameObject.Find("Game Manager").GetComponent<WeaponModules>().FireWeapon(altWeapon);
    }

    void ApplyModule(DragDrop obj, DragDrop slot)
    {
        bool isSlotted = DragDrop.allSlots.Contains(obj);
        InfoTag tag = (obj == null) ? null : obj.GetComponent<InfoTag>();

        if (obj.name == "Weapon") weapon = tag;
    }
}
