using MilanUtils;
using static MilanUtils.Variables;
using UnityEngine;
using System.Collections.Generic;

public class ModuleApplyHandler : MonoBehaviour
{
    public static Dictionary<string, List<string>> appliedItems = new();
    public static Dictionary<string, WeaponStats> allWeapons = new();
    public static Dictionary<string, ItemInfo> allItems = new();
    public static Dictionary<string, ItemInfo.WeaponModifiers> allModifiers = new();

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
            else if(obj.Value.GetType() == typeof(GameObject) && ((GameObject)obj.Value).TryGetComponent(out ItemInfo ii))
            {
                allItems.Add(((GameObject)obj.Value).name, ii);
                allModifiers.Add(((GameObject)obj.Value).name, ii.modifiers);
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

        //If the item is a weapon
        if (item.name == "Weapon")
        {
            //If the item is slotted, set player baseMainWeapon to the correct value, that being the WeaponStats resource with the weaponName. Otherwise, set to null
            if (isSlotted)
            {
                PlayerManager.baseMainWeapon = (WeaponStats)resources[item.GetComponent<WeaponInfo>().weaponName];
            }
            else
            {
                PlayerManager.baseMainWeapon = null;
            }
        }
        //If the item is an alt weapon
        else if(item.name == "Alt Weapon")
        {
            //If the item is slotted, set player baseMainWeapon to the correct value, that being the WeaponStats resource with the weaponName. Otherwise, set to null
            if (isSlotted)
            {
                PlayerManager.baseAltWeapon = (WeaponStats)resources[item.GetComponent<WeaponInfo>().weaponName];
            }
            else
            {
                PlayerManager.baseAltWeapon = null;
            }
        }
    }

    void SlotIn(DragDrop item, DragDrop slot)
    {
        //This needs explanation because of hierarchy in the inspector, which is as follows: Weapon Inventory Parent > (Background Panel > (Slots > (Items)), Weapon)
        //If is currently in a slot (which is in Background Panel, which is in Inventory Panel), add this to the appliedItems of the 2nd parent (Inventory Parent)'s name without " Inventory Parent"
        //Otherwise, if the new "slot" does contain slot in the name (so isn't a parent), remove it from appliedItems of the 2nd parent
        ItemInfo info = item.GetComponent<ItemInfo>();
        if (DragDrop.slottedItems.Contains(item) && !item.name.Contains("Weapon"))
            appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Add(info.name);
    }

    void SlotOut(DragDrop item, DragDrop slot)
    {
        //This needs explanation because of hierarchy in the inspector, which is as follows: Weapon Inventory Parent > (Background Panel > (Slots > (Items)), Weapon)
        //If is currently in a slot (which is in Background Panel, which is in Inventory Panel), add this to the appliedItems of the 2nd parent (Inventory Parent)'s name without " Inventory Parent"
        //Otherwise, if the new "slot" does contain slot in the name (so isn't a parent), remove it from appliedItems of the 2nd parent
        ItemInfo info = item.GetComponent<ItemInfo>();
        if(slot.transform.name.Contains("Slot") && !item.name.Contains("Weapon"))
            appliedItems[slot.transform.parent.parent.name[..slot.transform.parent.parent.name.IndexOf(" Inventory Parent")]].Remove(info.name);
    }
}
