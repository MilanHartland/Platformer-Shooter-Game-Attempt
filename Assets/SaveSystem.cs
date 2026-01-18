using System.Collections.Generic;
using System.IO;
using MilanUtils;
using UnityEngine;
using UnityEngine.UI;

//The reason BigEndian is used for encoding instead of nothing is because it looks fancy
public static class SaveSystem
{
    static SaveData saveData = new();

    static void print(object obj){Debug.Log(obj);}

    [RuntimeInitializeOnLoadMethod]
    static void Start()
    {
        if(!Keybinds.bindings.ContainsKey("Upgrade")) Keybinds.bindings.Add("Upgrade", KeyCode.Q);
    }

    public static void SetSaveData()
    {
        DragDrop.StopDragging();
        
        saveData = new();
        SaveData data = new();

        //For each slotitempair, if it is a weapon/altweapon/movement slot item, add it to equippedItems
        foreach(SlotItemPair pair in DragDrop.slotItemPairs)
        {
            SaveData.EquipData equipData = new()
            {
                slotName = pair.slot.gameObject.name,
                itemName = pair.item.gameObject.name
            };

            switch (pair.slot.name)
            {
                case "Weapon":
                case "Alt Weapon":
                case "Movement": //IF MOVEMENT SLOT GETS CHANGED IN NAME CHANGE IT HERE
                    data.equippedItems.Add(equipData);   
                    break;
            }
        }

        //For each weapon parent in inventory, add it to data
        foreach(Transform weaponParent in World.FindInactive("Weapon Inventory").transform)
        {
            //Gets the actual weapon, which is the child with the same name but without " Inventory Parent". If it is null, throw an error
            Transform weapon = weaponParent.Find(weaponParent.gameObject.name[..weaponParent.gameObject.name.IndexOf(" Inventory Parent")]);
            if(weapon == null) Debug.LogError($"No weapon of name {weaponParent.gameObject.name[..weaponParent.gameObject.name.IndexOf(" Inventory Parent")]} found in {weaponParent.gameObject.name}!");

            //Creates new WeaponData
            SaveData.WeaponData weaponData = new()
            {
                weaponName = weapon.gameObject.name
            };

            //Gets the background panel, then for each slot in it, if it is a bullet slot and has an item, add it to bulletNames. If it is an effect slot and has an item, add it to effectNames
            Transform backgroundPanel = weaponParent.Find("Background Panel");
            foreach(Transform slot in backgroundPanel)
            {
                if(slot.name.Contains("Bullet") && slot.childCount > 0) weaponData.bulletNames.Add(slot.GetChild(0).gameObject.name);
                else if(slot.name.Contains("Effect") && slot.childCount > 0) weaponData.effectNames.Add(slot.GetChild(0).gameObject.name);
            }

            //Add the weaponData to data
            data.weapons.Add(weaponData);
        }

        //Adds the bullet/effect names to the lists
        foreach(Transform bullet in World.FindInactive("Bullet Inventory").transform){data.bulletInventory.Add(bullet.name);}
        foreach(Transform effect in World.FindInactive("Effect Inventory").transform){data.effectInventory.Add(effect.name);}

        //Adds the keybind names and bindings to the lists
        foreach(var pair in Keybinds.bindings)
        {
            data.keybindNames.Add(pair.Key);
            data.keybindValues.Add(pair.Value);
        }

        //Sets the 'hold to drag' toggle
        data.ddHoldToggle = World.FindInactive("DragDrop Hold Toggle").GetComponent<Toggle>().isOn;

        //Sets saveData to the new data
        saveData = data;
    }

    public static void SaveToFile(bool runSetSaveData = true)
    {
        //If it should, create new save data
        if(runSetSaveData) SetSaveData();

        //Turns the saveData into a JSON string, writes it to the savefile and encrypts it (doesn't have to but it looks fancy)
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(Application.persistentDataPath + "/savefile.json", json, System.Text.Encoding.BigEndianUnicode);
        Debug.Log("Save");
    }

    public static void Load()
    {
        //Gets the text, then sets the saveData to the deserialized values
        string text = File.ReadAllText(Application.persistentDataPath + "/savefile.json", System.Text.Encoding.BigEndianUnicode);
        saveData = (SaveData)JsonUtility.FromJson(text, typeof(SaveData));
        Debug.Log("Load");

        DragDrop.slotItemPairs.Clear();
        DragDrop.slottedItems.Clear();
        DragDrop.usedSlots.Clear();
        ModuleApplyHandler.appliedItems.Clear();

        //Gets the weapon inventory and slot parent, and unslots/deletes all weapons from them
        Transform weaponInventory = World.FindInactive("Weapon Inventory").transform;
        
        //Destroys all children of weapon inventory
        for(int i = weaponInventory.childCount - 1; i >= 0; i--) Object.Destroy(weaponInventory.GetChild(i).gameObject);
        foreach(Transform slot in World.FindInactive("Slot Parent").transform)
        {
            for(int i = slot.childCount - 1; i >= 0; i--) 
            {
                DragDrop.UnSlot(slot.GetChild(i).gameObject);
                Object.Destroy(slot.GetChild(i).gameObject);
            }
        }
        
        //Loads the data from the weapons and equipment
        foreach(var weaponData in saveData.weapons)
        {
            //Instantiates the weapon object and throws an error if nothing found
            GameObject weapon = Object.Instantiate(Variables.prefabs[weaponData.weaponName]);
            if(weapon == null) Debug.LogError($"Cannot Instantiate {weaponData.weaponName}!");

            //Sets the weapon name to avoid it containing (Clone), sets the parent, generates an edit layout, and adds it back to appliedItems
            weapon.name = weaponData.weaponName;
            weapon.transform.SetParent(weaponInventory);
            weapon.GetComponent<WeaponInfo>().ResetEditLayout();
            ModuleApplyHandler.appliedItems.Add(weapon.name, new());

            //Adds the bullets to the slots
            foreach(var bulletName in weaponData.bulletNames)
            {
                //Creates a bullet and throws an error if it didn't
                GameObject bullet = Object.Instantiate(Variables.prefabs[bulletName]);
                if(bullet == null) Debug.LogError($"Cannot Instantiate {bulletName}!");

                //Sets the name to avoid (Clone), sets the parent to the weapon parent (temporarily) and resets localScale
                bullet.name = bulletName;
                bullet.transform.SetParent(weapon.transform.parent);
                bullet.transform.localScale = Vector3.one;
                
                //Gets the backgroundPanel, then gets each slot in the panel. If the slot is a bullet slot and isn't used yet, run Start() (for fixing type) and slot it in
                Transform backgroundPanel = weapon.transform.parent.Find("Background Panel");
                foreach(Transform child in backgroundPanel)
                {
                    if(child.name.Contains("Bullet") && !DragDrop.usedSlots.Contains(child.GetComponent<DragDrop>()))
                    {
                        child.GetComponent<DragDrop>().Start();
                        DragDrop.Slot(bullet, child.gameObject);
                    }
                }
            }

            //Adds the bullets to the slots
            foreach(var effectName in weaponData.effectNames)
            {
                //Creates an effect and throws an error if it didn't
                GameObject effect = Object.Instantiate(Variables.prefabs[effectName]);
                if(effect == null) Debug.LogError($"Cannot Instantiate {effectName}!");

                //Sets the name to avoid (Clone), sets the parent to the weapon parent (temporarily) and resets localScale
                effect.name = effectName;
                effect.transform.SetParent(weapon.transform.parent);
                effect.transform.localScale = Vector3.one;
                
                //Gets the backgroundPanel, then gets each slot in the panel. If the slot is an effect slot and isn't used yet, run Start() (for fixing type) and slot it in
                Transform backgroundPanel = weapon.transform.parent.Find("Background Panel");
                foreach(Transform child in backgroundPanel)
                {
                    if(child.name.Contains("Effect") && !DragDrop.usedSlots.Contains(child.GetComponent<DragDrop>()))
                    {
                        child.GetComponent<DragDrop>().Start();
                        DragDrop.Slot(effect, child.gameObject);
                    }
                }
            }

            //Equips the items
            foreach(var equip in saveData.equippedItems)
            {
                //If the name of the equipped item is a copy of the weapon name (because spawner), spawn a weapon and slot it in
                if(equip.itemName == weapon.name + " Copy")
                {
                    //Gets the DragDrop and throws an error if it didn't
                    DragDrop dd = weapon.GetComponent<DragDrop>();
                    if(dd == null) Debug.LogError($"Instantiated weapon {weapon} does not have DragDrop!");
                    
                    //Same code as in DragDrop.cs that creates a new copy of the weapon and slots it
                    DragDrop slot = World.FindInactive(equip.slotName).GetComponent<DragDrop>();
                    GameObject copy = Object.Instantiate(weapon);
                    copy.transform.SetParent(slot.transform);
                    copy.transform.localScale = slot.transform.InverseTransformVector(weapon.transform.lossyScale);
                    copy.GetComponent<RectTransform>().sizeDelta = weapon.GetComponent<RectTransform>().sizeDelta;
                    copy.GetComponent<DragDrop>().type = DragDrop.DragDropType.Copy;
                    copy.name = weapon.transform.name + " Copy";
                    copy.GetComponent<DragDrop>().parent = null;
                    dd.spawnerInstances.Add(copy);

                    DragDrop.Slot(copy, slot.gameObject);
                }
            }
        }
        
        //Gets the bullet/effect inventory objects and clears all children
        Transform bulletInventory = World.FindInactive("Bullet Inventory").transform;
        Transform effectInventory = World.FindInactive("Effect Inventory").transform;
        for(int i = bulletInventory.childCount - 1; i >= 0; i--){Object.Destroy(bulletInventory.GetChild(i).gameObject);}
        for(int i = effectInventory.childCount - 1; i >= 0; i--){Object.Destroy(effectInventory.GetChild(i).gameObject);}

        //For each bullet/effect, instantiate it, set the name to avoid (Copy), set parent to the inventory, and reset localScale
        foreach(var bullet in saveData.bulletInventory)
        {
            GameObject obj = Object.Instantiate(Variables.prefabs[bullet]);
            obj.name = bullet;
            obj.transform.SetParent(bulletInventory);
            obj.transform.localScale = Vector3.one;
        }
        foreach(var effect in saveData.effectInventory)
        {
            GameObject obj = Object.Instantiate(Variables.prefabs[effect]);
            obj.name = effect;
            obj.transform.SetParent(effectInventory);
            obj.transform.localScale = Vector3.one;
        }

        //Clears the keybind list, then adds the names and bindings back, then it finds the associated UI object to run SetTextUI on the BindingsChanger
        Keybinds.bindings.Clear();
        for(int i = 0; i < saveData.keybindNames.Count; i++)
        {
            Keybinds.bindings.Add(saveData.keybindNames[i], saveData.keybindValues[i]);
            foreach(Transform obj in World.FindInactive("Keybind Parent").transform)
            {
                if(obj.TryGetComponent(out BindingsChanger bc)) bc.SetTextUI();
            }
        }

        //Sets the toggle state
        World.FindInactive("DragDrop Hold Toggle").GetComponent<Toggle>().isOn = saveData.ddHoldToggle;
    }

    public static void ResetSave()
    {
        saveData.equippedItems = new();
        saveData.weapons = new();
        saveData.bulletInventory = new();
        saveData.effectInventory = new();
        saveData.ddHoldToggle = true;
        SaveToFile(false);
    }

    [System.Serializable]
    public class SaveData
    {
        public List<EquipData> equippedItems = new();
        public List<WeaponData> weapons = new();
        public List<string> bulletInventory = new();
        public List<string> effectInventory = new();

        public List<string> keybindNames = new();
        public List<KeyCode> keybindValues = new();

        public bool ddHoldToggle = true;

        public SaveData(){}

        [System.Serializable]
        public class WeaponData
        {
            public string weaponName;
            public List<string> bulletNames = new();
            public List<string> effectNames = new();
        }

        [System.Serializable]
        public class EquipData
        {
            public string slotName;
            public string itemName;
        }
    }
}
