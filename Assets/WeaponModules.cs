using MilanUtils;
using UnityEngine;

public class WeaponModules : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject triggerSlotPrefab, arrowFillerPrefab, effectSlotPrefab, triggerEffectPairPrefab;

    Transform player;

    void Start()
    {
        player = GameObject.Find("Player").transform;
    }

    public void FireWeapon(InfoTag weapon)
    {
        switch (weapon.name)
        {
            case "Pistol":
                GameObject obj = GameObject.Instantiate(bulletPrefab);
                Angle2D.TurnTo(obj, World.mousePos);
                obj.transform.position = player.position;
                obj.GetComponent<Rigidbody2D>().linearVelocity = Angle2D.GetAngleFromPos<GameObject, Vector2>(obj, World.mousePos) * 10f;
                break;
        }
    }
}
