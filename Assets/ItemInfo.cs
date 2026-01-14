using System.Collections.Generic;
using MilanUtils;
using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    public enum ItemType{Bullet, Effect}
    [Tooltip("The type of item this is. Bullets modify the trajectory and behaviour of bullets, while effects do other things")]public ItemType type;

    public new string name;
    [SerializeField, TextArea, Tooltip("The description of the item. Add [d], [f], [m], [r], [s], or [v] to have the shown string show the modifier value of" +
    "damage, fire rate, magazine size, reload time, spread, and speed respectively. Add a percentage symbol behind the letter to get the +x% percentage")] string Description;
    public string description {get{return ReplaceDescriptionValues();}}

    [Tooltip("The priority of the effect. Lower values get triggered first")]public int effectOrder;
    
    [Tooltip("The stat modifiers this item applies to a weapon. For example, a damage modifier of 1.5 means the weapon deals +50% damage")]
    public WeaponModifiers modifiers = WeaponModifiers.One;

    [Tooltip("The values this item has. For possible use in BulletBehaviour")]public EditorDictionary<string, float> itemValues;
    [Tooltip("The upgrade info of this item")]public List<UpgradeInfo> upgrades;

    void Update()
    {
        if(Input.GetKeyDown(Keybinds.bindings["Upgrade"]) && upgrades.Count > 0 && UI.GetObjectsUnderMouse().Contains(gameObject))
        {
            //DO SOMETHING HERE TO DECREASE CURRENCY BASED ON COST

            itemValues = upgrades[0].itemValues;
            modifiers += upgrades[0].modifiers;
            upgrades.RemoveAt(0);
        }
    }

    string ReplaceDescriptionValues()
    {
        string fullDesc = Description.Replace("[d]", modifiers.damageModifier.ToString());
        fullDesc = fullDesc.Replace("[f]", modifiers.fireRateModifier.ToString());
        fullDesc = fullDesc.Replace("[m]", modifiers.magazineModifier.ToString());
        fullDesc = fullDesc.Replace("[r]", modifiers.reloadModifier.ToString());
        fullDesc = fullDesc.Replace("[s]", modifiers.spreadModifier.ToString());
        fullDesc = fullDesc.Replace("[v]", modifiers.speedModifier.ToString());
        fullDesc = fullDesc.Replace("[d%]", GetModifierPercentageText(modifiers.damageModifier));
        fullDesc = fullDesc.Replace("[f%]", GetModifierPercentageText(modifiers.fireRateModifier));
        fullDesc = fullDesc.Replace("[m%]", GetModifierPercentageText(modifiers.magazineModifier));
        fullDesc = fullDesc.Replace("[r%]", GetModifierPercentageText(modifiers.reloadModifier));
        fullDesc = fullDesc.Replace("[s%]", GetModifierPercentageText(modifiers.spreadModifier));
        fullDesc = fullDesc.Replace("[v%]", GetModifierPercentageText(modifiers.speedModifier));
        foreach(var value in itemValues.list)
        {
            fullDesc = fullDesc.Replace($"[{value.Key}]", value.Value.ToString());
        }
        return fullDesc;
    }

    [ContextMenu("Set Modifier Text")]
    void SetBaseModifierText()
    {
        string text = string.Empty;
        if(modifiers.forceNonAutomatic) text += "Manual\n";
        else if(modifiers.forceAutomatic) text += "Automatic\n";
        if(modifiers.damageModifier != 1f) text += $"{GetModifierPercentageText(modifiers.damageModifier)} damage\n";
        if(modifiers.fireRateModifier != 1f) text += $"{GetModifierPercentageText(modifiers.fireRateModifier)} bullets per second\n";
        if(modifiers.magazineModifier != 1f) text += $"{GetModifierPercentageText(modifiers.magazineModifier)} magazine size\n";
        if(modifiers.reloadModifier != 1f) text += $"{GetModifierPercentageText(modifiers.reloadModifier)} reload time\n";
        if(modifiers.spreadModifier != 1f) text += $"{GetModifierPercentageText(modifiers.spreadModifier)} spread\n";
        if(modifiers.speedModifier != 1f) text += $"{GetModifierPercentageText(modifiers.speedModifier)} bullet speed";
        Description = text;
    }

    [ContextMenu("Log description")]void LogDescription(){Debug.Log(description);}

    public string GetModifierPercentageText(float mod)
    {
        return $"{(Mathf.Sign(mod - 1f) < 0f ? string.Empty : "+")}{(mod - 1) * 100f}%";
    }

    void OnValidate()
    {
        //Sets DragDrop name to the type
        if(TryGetComponent(out DragDrop dd)) dd.name = type.ToString();
        name = gameObject.name;
        
        //For each upgrade, validate itemValues
        for(int i = 0; i < upgrades.Count; i++)
        {
            //If itemValues count is too low, add new itemValues
            if(upgrades[i].itemValues.list.Count < itemValues.list.Count)
            {
                for(int x = 0; x < itemValues.list.Count - upgrades[i].itemValues.list.Count; x++)
                    upgrades[i].itemValues.list.Add(itemValues.list[x]);
            }
            //If itemValues count is too high, remove itemValues
            else if(upgrades[i].itemValues.list.Count > itemValues.list.Count)
            {
                if(itemValues.list.Count != 0)
                    upgrades[i].itemValues.list.RemoveRange(itemValues.list.Count - 1, upgrades[i].itemValues.list.Count - itemValues.list.Count);
                else upgrades[i].itemValues.list.Clear();
            }
            
            //For each value, set the key name to the item's itemValue key name (keep the same value)
            for(int j = 0; j < upgrades[i].itemValues.list.Count; j++)
            {
                upgrades[i].itemValues.list[j] = new(itemValues.list[j].Key, upgrades[i].itemValues.list[j].Value);
            }
        }
    }

    [System.Serializable]
    public struct WeaponModifiers
    {
        public bool forceNonAutomatic;
        public bool forceAutomatic;
        public float damageModifier;
        public float fireRateModifier;
        public float magazineModifier;
        public float reloadModifier;
        public float spreadModifier;
        public float speedModifier;

        public static WeaponModifiers One = new(){damageModifier = 1f, fireRateModifier = 1f, magazineModifier = 1f, reloadModifier = 1f, spreadModifier = 1f, speedModifier = 1f};

        public static WeaponModifiers operator +(WeaponModifiers a, WeaponModifiers b)
        {
            return new()
            {
                forceNonAutomatic = (a.forceNonAutomatic || b.forceNonAutomatic) && !(a.forceAutomatic || b.forceAutomatic),  
                forceAutomatic = a.forceAutomatic || b.forceAutomatic,
                damageModifier = a.damageModifier + b.damageModifier,
                fireRateModifier = a.fireRateModifier + b.fireRateModifier,
                magazineModifier = a.magazineModifier + b.magazineModifier,
                reloadModifier = a.reloadModifier + b.reloadModifier,
                spreadModifier = a.spreadModifier + b.spreadModifier,
                speedModifier = a.speedModifier + b.speedModifier,
            };
        }
    }

    //IDEA ON HOW TO IMPLEMENT THE UPGRADES: Have a custom class (UpgradeInfo) that has a cost, description, and an editor dictionary of string/float values.
    //The values get used by BulletBehaviour for whatever it needs to (for example: a magnetism item can increase magnetism with a ("Magnetism", 5f) keyvaluepair)
    [System.Serializable]
    public struct UpgradeInfo
    {
        [Tooltip("The cost to buy this upgrade")]public int cost;
        [Tooltip("The description this specific upgrade has"), TextArea]public string description;
        [Tooltip("The modifiers added to the item when upgraded (additive)")]public WeaponModifiers modifiers;
        [Tooltip("The values the item takes after the upgrade")]public EditorDictionary<string, float> itemValues;
    }
}
