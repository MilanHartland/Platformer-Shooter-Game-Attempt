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
        if(TryGetComponent(out DragDrop dd)) dd.name = type.ToString();
        name = gameObject.name;
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
}
