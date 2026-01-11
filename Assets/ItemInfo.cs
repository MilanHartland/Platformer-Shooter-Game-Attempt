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

    [Tooltip("The values this item has. For possible use in BulletBehaviour")]public EditorDictionary<string, float> itemValues;
    [Tooltip("The upgrade info of this item")]public List<UpgradeInfo> upgrades;
    
    [Tooltip("The stat modifiers this item applies to a weapon. For example, a damage modifier of 1.5 means the weapon deals +50% damage")]
    public WeaponModifiers modifiers = WeaponModifiers.One;

    string ReplaceDescriptionValues()
    {
        string damageText = modifiers.damageModifier.ToString();
        string fireRateText = modifiers.fireRateModifier.ToString();
        string magazineText = modifiers.magazineModifier.ToString();
        string reloadText = modifiers.reloadModifier.ToString();
        string spreadText = modifiers.spreadModifier.ToString();
        string speedText = modifiers.speedModifier.ToString();

        string fullDesc = Description.Replace("[d]", damageText);
        fullDesc = Description.Replace("[f]", fireRateText);
        fullDesc = Description.Replace("[m]", magazineText);
        fullDesc = Description.Replace("[r]", reloadText);
        fullDesc = Description.Replace("[s]", spreadText);
        fullDesc = Description.Replace("[v]", speedText);
        fullDesc = Description.Replace("[d%]", GetModifierPercentageText(modifiers.damageModifier));
        fullDesc = Description.Replace("[f%]", GetModifierPercentageText(modifiers.fireRateModifier));
        fullDesc = Description.Replace("[m%]", GetModifierPercentageText(modifiers.magazineModifier));
        fullDesc = Description.Replace("[r%]", GetModifierPercentageText(modifiers.reloadModifier));
        fullDesc = Description.Replace("[s%]", GetModifierPercentageText(modifiers.spreadModifier));
        fullDesc = Description.Replace("[v%]", GetModifierPercentageText(modifiers.speedModifier));
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
    }

    //IDEA ON HOW TO IMPLEMENT THE UPGRADES: Have a custom class (UpgradeInfo) that has a cost, description, and an editor dictionary of string/float values.
    //The values get used by BulletBehaviour for whatever it needs to (for example: a magnetism item can increase magnetism with a ("Magnetism", 5f) keyvaluepair)
    [System.Serializable]
    public struct UpgradeInfo
    {
        [Tooltip("The cost to buy this upgrade")]public int cost;
        [Tooltip("The description this specific upgrade has"), TextArea]public string description;
        [Tooltip("The values the item takes after the upgrade")]public EditorDictionary<string, float> itemValues;
    }
}
