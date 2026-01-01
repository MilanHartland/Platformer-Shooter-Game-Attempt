using UnityEngine;
using MilanUtils;

[CreateAssetMenu]
public class WeaponStats : ScriptableObject
{
    public enum FiringType{Projectile, Hitscan}

    [Tooltip("The name of the weapon")] public new string name;
    [Tooltip("If the gun is automatic (hold to fire) instead of manual (click to fire)")] public bool automatic;
    [Tooltip("The damage every hit does")] public float damage;
    [Tooltip("The fire rate in bullets/second")] public float fireRate;
    [Tooltip("The amount of bullets in a single magazine")] public int magazineSize;
    [Tooltip("The time it takes to reload")] public float reloadTime;
    [Tooltip("The spread in degrees")] public float spread;
    [Tooltip("The type of firing the gun does\n\nProjectile: shoots a physical projectile with gravity\nHitscan: shoots a raycast")] public FiringType firingType;
    [Tooltip("The speed of the bullet"), ShowIf(_enumName: "firingType", "Projectile")] public float bulletSpeed;
    [Tooltip("The maximum distance the hitscan can go"), ShowIf(_enumName: "firingType", "Hitscan"), SetName("Max Distance")] public float maxHitscanDistance;

    void OnValidate()
    {
        if(name == string.Empty) name = ((UnityEngine.Object)this).name;
    }
}
