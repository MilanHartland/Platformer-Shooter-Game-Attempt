using System.Collections.Generic;
using MilanUtils;
using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    public enum ItemType{Bullet, Effect}
    [Tooltip("The type of item this is. Bullets modify the trajectory and behaviour of bullets, while effects do other things")]public ItemType type;

    public new string name;
    [SerializeField, TextArea, Tooltip("The description of the item. Add [d], [f], [m], [r], [b], [s], or [v] to have the shown string show the modifier value of" +
    "damage, fire rate, magazine size, reload time, bullets, spread, and speed respectively. Add a percentage symbol behind the letter to get the +x% percentage")] string Description;
    public string description {get{return ReplaceDescriptionValues();}}

    [Tooltip("The priority of the effect. Lower values get triggered first")]public int effectOrder;
    
    [Tooltip("The stat modifiers this item applies to a weapon. For example, a damage modifier of 1.5 means the weapon deals +50% damage")]
    public WeaponModifiers modifiers = WeaponModifiers.One;

    [Tooltip("The values this item has. For possible use in BulletBehaviour")]public EditorDictionary<string, float> itemValues;
    [Tooltip("The upgrade info of this item")]public List<UpgradeInfo> upgrades;

    void Update()
    {
        //If pressing the upgrade bind, and there is an upgrade for which the cost is met, and the mouse is over this object: set item variables of upgrade, pay, then remove it from the upgrades list
        if(Input.GetKeyDown(Keybinds.bindings["Upgrade"]) && upgrades.Count > 0 && PlayerManager.oreCount >= upgrades[0].cost && UI.GetObjectsUnderMouse().Contains(gameObject))
        {
            itemValues = upgrades[0].itemValues;
            modifiers += upgrades[0].modifiers;
            if(upgrades[0].itemDescription != string.Empty) Description = upgrades[0].itemDescription;
            PlayerManager.oreCount -= upgrades[0].cost;
            upgrades.RemoveAt(0);
        }
    }

    string ReplaceDescriptionValues()
    {
        string fullDesc = Description.Replace("[d]", modifiers.damageModifier.ToString());
        fullDesc = fullDesc.Replace("[f]", modifiers.fireRateModifier.ToString());
        fullDesc = fullDesc.Replace("[m]", modifiers.magazineModifier.ToString());
        fullDesc = fullDesc.Replace("[r]", modifiers.reloadModifier.ToString());
        fullDesc = fullDesc.Replace("[b]", modifiers.bulletCountModifier.ToString());
        fullDesc = fullDesc.Replace("[s]", modifiers.spreadModifier.ToString());
        fullDesc = fullDesc.Replace("[v]", modifiers.speedModifier.ToString());
        fullDesc = fullDesc.Replace("[d%]", GetModifierPercentageText(modifiers.damageModifier));
        fullDesc = fullDesc.Replace("[f%]", GetModifierPercentageText(modifiers.fireRateModifier));
        fullDesc = fullDesc.Replace("[m%]", GetModifierPercentageText(modifiers.magazineModifier));
        fullDesc = fullDesc.Replace("[r%]", GetModifierPercentageText(modifiers.reloadModifier));
        fullDesc = fullDesc.Replace("[b%]", GetModifierPercentageText(modifiers.bulletCountModifier));
        fullDesc = fullDesc.Replace("[s%]", GetModifierPercentageText(modifiers.spreadModifier));
        fullDesc = fullDesc.Replace("[v%]", GetModifierPercentageText(modifiers.speedModifier));
        foreach(var value in itemValues.list)
        {
            fullDesc = fullDesc.Replace($"[{value.Key}]", value.Value.ToString());
            fullDesc = fullDesc.Replace($"[{value.Key}%]", $"{value.Value * 100f}%");
        }
        return fullDesc;
    }

    [ContextMenu("Set Modifier Text")]
    void SetBaseModifierText()
    {
        string text = string.Empty;
        if(modifiers.automaticModifier == WeaponModifiers.ForceAutomaticType.MakeManual) text += "Manual\n";
        else if(modifiers.automaticModifier == WeaponModifiers.ForceAutomaticType.MakeAutomatic) text += "Automatic\n";
        if(modifiers.damageModifier != 1f) text += $"[d%] damage\n";
        if(modifiers.reloadModifier != 1f) text += $"[b%] bullets per shot\n";
        if(modifiers.fireRateModifier != 1f) text += $"[f%] bullets per second\n";
        if(modifiers.magazineModifier != 1f) text += $"[m%] magazine size\n";
        if(modifiers.reloadModifier != 1f) text += $"[r%] reload time\n";
        if(modifiers.spreadModifier != 1f) text += $"[s%] spread\n";
        if(modifiers.speedModifier != 1f) text += $"[v%] bullet speed";
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

    public override string ToString(){return name;}

    [System.Serializable]
    public struct WeaponModifiers
    {
        public enum ForceAutomaticType{Keep, MakeManual, MakeAutomatic}
        public ForceAutomaticType automaticModifier;
        public float damageModifier;
        public float bulletCountModifier;
        public float fireRateModifier;
        public float magazineModifier;
        public float reloadModifier;
        public float spreadModifier;
        public float speedModifier;

        public static WeaponModifiers One = new()
        {damageModifier = 1f, fireRateModifier = 1f, bulletCountModifier = 1f, magazineModifier = 1f, reloadModifier = 1f, spreadModifier = 1f, speedModifier = 1f};

        public static WeaponModifiers operator +(WeaponModifiers a, WeaponModifiers b)
        {
            return new()
            {
                automaticModifier = a.automaticModifier,
                damageModifier = a.damageModifier + b.damageModifier,
                bulletCountModifier = a.bulletCountModifier + b.bulletCountModifier,
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
        [Tooltip("The description this specific upgrade has"), TextArea]public string upgradeDescription;
        [Tooltip("The description that the item will get after the upgrade")]public string itemDescription;
        [Tooltip("The modifiers added to the item when upgraded (additive)")]public WeaponModifiers modifiers;
        [Tooltip("The values the item takes after the upgrade")]public EditorDictionary<string, float> itemValues;
    }
}
