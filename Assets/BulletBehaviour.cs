using UnityEngine;
using MilanUtils;
using System.Collections;

[RequireComponent(typeof(TrailRenderer))]
public class BulletBehaviour : MonoBehaviour
{
    public WeaponStats shotBy;

    TrailRenderer trailRend;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trailRend = GetComponent<TrailRenderer>();
    }

    void LateUpdate()
    {
        if(trailRend.enabled == false) trailRend.enabled = true;
    }

    void OnEnable()
    {
        if(!trailRend) Start();
        
        StopCoroutine(nameof(DisableCoroutine));
    }

    void OnDisable()
    {
        if(!trailRend) Start();
        trailRend.Clear();
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if(coll.gameObject.CompareTag("Map"))
        {
            gameObject.SetActive(false);
        }
        else if(coll.gameObject.CompareTag("Enemy"))
        {
            EnemyBehaviour enemyBehaviour = coll.gameObject.GetComponent<EnemyBehaviour>();
            enemyBehaviour.hp -= shotBy.damage;
            DamageText.SpawnDamageText(coll.gameObject, shotBy.damage);
            
            gameObject.SetActive(false);
        }
    }

    public void Disable(float afterSeconds){StartCoroutine(DisableCoroutine(afterSeconds));}
    IEnumerator DisableCoroutine(float afterSeconds){yield return Variables.GetWaitForSeconds(afterSeconds); gameObject.SetActive(false);}
}
