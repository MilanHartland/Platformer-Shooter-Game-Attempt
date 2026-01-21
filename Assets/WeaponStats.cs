using UnityEngine;
using MilanUtils;
using UnityEngine.UI;

#pragma warning disable CS0660
#pragma warning disable CS0661
[CreateAssetMenu]
public class WeaponStats : ScriptableObject
{
    public enum FiringType{Projectile, Hitscan}

    [Tooltip("The name of the weapon")] public new string name;
    [Tooltip("If the gun is automatic (hold to fire) instead of manual (click to fire)")] public bool automatic;
    [Tooltip("The damage every hit does")] public float damage;
    [Tooltip("The amount of bullets shot at once. Value is the percentage to shoot a bullet, so 1.4 is 100% chance at 1 bullet, and 40% of 2 bullets")] public float bulletCount;
    [Tooltip("The fire rate in bullets/second")] public float fireRate;
    [Tooltip("The amount of bullets in a single magazine")] public int magazineSize;
    [Tooltip("The time it takes to reload")] public float reloadTime;
    [Tooltip("The spread in degrees")] public float spread;
    [Tooltip("The type of firing the gun does\n\nProjectile: shoots a physical projectile with gravity\nHitscan: shoots a raycast. Should only be used for enemy guns")] public FiringType firingType;
    [Tooltip("The speed of the bullet"), ShowIf(_enumName: "firingType", "Projectile")] public float bulletSpeed;
    [Tooltip("The maximum distance the hitscan can go"), ShowIf(_enumName: "firingType", "Hitscan"), SetName("Max Distance")] public float maxHitscanDistance;
    [Tooltip("The sprite that held guns will have")]public Sprite gunImage;

    public void SetValues(WeaponStats w)
    {name = w.name; automatic = w.automatic; damage = w.damage; bulletCount = w.bulletCount; fireRate = w.fireRate; 
    magazineSize = w.magazineSize; reloadTime = w.reloadTime; spread = w.spread; bulletSpeed = w.bulletSpeed;}

    void OnValidate()
    {
        if(name == string.Empty) name = ((Object)this).name;
        if(bulletCount < 0) bulletCount = 0f;
    }

    public WeaponStats AddModifiers(ItemInfo.WeaponModifiers mod)
    {
        WeaponStats w = CreateInstance<WeaponStats>();
        w.SetValues(this);

        if(mod.automaticModifier == ItemInfo.WeaponModifiers.ForceAutomaticType.MakeAutomatic) w.automatic = true;
        else if(mod.automaticModifier == ItemInfo.WeaponModifiers.ForceAutomaticType.MakeManual) w.automatic = false;
        
        w.damage *= mod.damageModifier;
        w.bulletCount *= mod.bulletCountModifier;
        w.fireRate *= mod.fireRateModifier;
        w.magazineSize = Mathf.FloorToInt(w.magazineSize * mod.magazineModifier);
        w.reloadTime *= mod.reloadModifier;
        w.spread *= mod.spreadModifier;
        w.bulletSpeed *= mod.speedModifier;
        return w;
    }

    public static bool operator ==(WeaponStats a, WeaponStats b){return ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && !ReferenceEquals(b, null) && a.name == b.name);;}
    public static bool operator !=(WeaponStats a, WeaponStats b){return !(a == b);}
}
