using MilanUtils;
using static MilanUtils.Objects;
using UnityEngine;

public class ModuleEffectHandler : MonoBehaviour
{
    InfoTag weapon, altWeapon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += ApplyModule;
        DragDrop.dragOutAction += ApplyModule;

        TryAutoSetValues();
        LoadAllResourcesToPrefabs();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameObject canvas = World.GameObjectWhere(x => { return x.name == "Canvas"; }, true);
            canvas.SetActive(!canvas.activeSelf);
        }

        if (Input.GetMouseButtonDown(0))
        {
            GameObject obj = Instantiate(prefabs["Bullet"]);
            Angle2D.TurnTo(obj, World.mousePos);
            obj.transform.position = player.position;
            obj.GetComponent<Rigidbody2D>().linearVelocity = Angle2D.GetAngleFromPos<GameObject, Vector2>(obj, World.mousePos) * 10f;
            Destroy(obj, 5f);
        }
    }

    void ApplyModule(DragDrop obj, DragDrop slot)
    {
        bool isSlotted = DragDrop.allSlots.Contains(obj);
        InfoTag tag = (obj == null) ? null : obj.GetComponent<InfoTag>();

        if (obj.name == "Weapon") weapon = tag;
    }
}
