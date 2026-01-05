using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine;
using System.Collections.Generic;

public class ModuleApplyHandler : MonoBehaviour
{
    public static Dictionary<string, List<string>> appliedItems = new();
    public static Dictionary<string, WeaponStats> allWeapons = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DragDrop.dragInAction += ApplyModule;
        DragDrop.dragInAction += SlotIn;
        DragDrop.dragOutAction += ApplyModule;
        DragDrop.dragOutAction += SlotOut;

        LoadAllResources();

        appliedItems = new();
        foreach(var obj in resources)
        {
            if(obj.Value.GetType() == typeof(WeaponStats))
            {
                appliedItems.Add(((WeaponStats)obj.Value).name, new());
                allWeapons.Add(((WeaponStats)obj.Value).name, (WeaponStats)obj.Value);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(MenuManager.IsPaused || MissionManager.inHub) return;
    }

    void ApplyModule(DragDrop item, DragDrop slot)
    {
        bool isSlotted = DragDrop.slottedItems.Contains(item);
        InfoTag tag = item.GetComponent<InfoTag>();

        if (item.name == "Weapon")
        {
            if (isSlotted)
            {
                PlayerManager.mainWeapon = (WeaponStats)resources[tag.name];
                PlayerManager.mainWeaponTimer = new(1f / PlayerManager.mainWeapon.fireRate);
            }
            else
            {
                PlayerManager.mainWeapon = null;
            }
        }
        else if(item.name == "Alt Weapon")
        {
            if (isSlotted)
            {
                PlayerManager.altWeapon = (WeaponStats)resources[tag.name];
                PlayerManager.altWeaponTimer = new(1f / PlayerManager.altWeapon.fireRate);
            }
            else
            {
                PlayerManager.altWeapon = null;
            }
        }
        else if (item.name == "Bullet" || item.name == "Effect")
        {
            //This needs explanation because of hierarchy in the inspector, which is as follows: Weapon Inventory Parent > (Background Panel > (Slots > (Items)), Weapon)
            //If is currently in a slot (which is in Background Panel, which is in Inventory Panel), add this to the appliedItems of the 2nd parent (Inventory Parent)'s name without " Inventory Parent"
            //Otherwise, if the new "slot" does contain slot in the name (so isn't a parent), remove it from appliedItems of the 2nd parent
            // if (isSlotted)
            //     appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Add(tag.name);
            // else if(slot.transform.name.Contains("Slot"))
            //     appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Remove(tag.name);
        }
    }

    void SlotIn(DragDrop item, DragDrop slot)
    {
        InfoTag tag = item.GetComponent<InfoTag>();
        if (DragDrop.slottedItems.Contains(item) && !item.name.Contains("Weapon"))
            appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Add(tag.name);
    }

    void SlotOut(DragDrop item, DragDrop slot)
    {
        InfoTag tag = item.GetComponent<InfoTag>();
        if(slot.transform.name.Contains("Slot") && !item.name.Contains("Weapon"))
            appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Remove(tag.name);
    }
}
