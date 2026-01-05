using UnityEngine;
using MilanUtils;
using System.Collections;

public class BulletBehaviour : MonoBehaviour
{
    public WeaponStats shotBy;

    TrailRenderer trailRend;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trailRend = GetComponent<TrailRenderer>();
    }

    void OnEnable()
    {
        if(!trailRend) Start();
        
        StopCoroutine("DisableCoroutine");
    }

    void OnDisable()
    {
        if(!trailRend) Start();
        trailRend.Clear();
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if(coll.gameObject.tag == "Map")
        {
            gameObject.SetActive(false);
        }
        else if(coll.gameObject.tag == "Enemy")
        {
            EnemyBehaviour enemyBehaviour = coll.gameObject.GetComponent<EnemyBehaviour>();
            enemyBehaviour.hp -= shotBy.damage;
            
            gameObject.SetActive(false);
        }
    }

    public void Disable(float afterSeconds){StartCoroutine(DisableCoroutine(afterSeconds));}
    IEnumerator DisableCoroutine(float afterSeconds){yield return Variables.GetWaitForSeconds(afterSeconds); gameObject.SetActive(false);}
}
