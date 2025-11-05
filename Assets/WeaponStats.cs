using UnityEngine;

[CreateAssetMenu]
public class WeaponStats : ScriptableObject
{
    [Tooltip("If the gun is automatic (hold to fire) instead of manual (click to fire)")] public bool automatic;
    [Tooltip("The fire rate in bullets/second")] public float fireRate;
    [Tooltip("The speed of the bullet")] public float bulletSpeed;
    [Tooltip("The spread in degrees")] public float spread;
}
