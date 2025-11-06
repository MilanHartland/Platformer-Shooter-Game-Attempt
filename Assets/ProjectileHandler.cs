using MilanUtils;
using static ModuleEffectHandler;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProjectileHandler : MonoBehaviour
{
    public WeaponStats shotBy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TriggerEffect(EffectTrigger.Fire, gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TriggerEffect(EffectTrigger.PhysicsFrame, gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TriggerEffect(EffectTrigger.Hit, gameObject, collision);
        
        // if(collision.gameObject.name == "Map")
        // {
        //     Vector3Int tileInt = World.ClosestTile(collision.GetContact(0).point, collision.gameObject.GetComponent<Tilemap>());

        //     if (!collision.gameObject.GetComponent<Tilemap>().GetTile(tileInt)) return;
        //     collision.gameObject.GetComponent<Tilemap>().SetTile(tileInt, null);
        //     GameObject obj = Instantiate(Objects.prefabs["Square"]);
        //     obj.transform.position = tileInt;
        //     obj.GetComponent<SpriteRenderer>().sprite = Sprite.Create((Texture2D)Objects.resources["Box"], new Rect(), Vector2.zero);
        //     Objects.Disintegrate(obj, (x) => { x.layer = LayerMask.NameToLayer("Entity"); });
        //     Destroy(this.gameObject);
        // }
    }
}
