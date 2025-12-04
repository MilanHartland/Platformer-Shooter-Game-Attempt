using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(EnemyPathfinding))]
public class EnemyBehaviour : MonoBehaviour
{
    RaycastHit2D[] visionCasts = new RaycastHit2D[5];
    List<RaycastHit2D> peerVisionCasts = new();

    bool seesPlayer;
    LayerMask mask;

    EnemyPathfinding pathfinding;

    Vector3 lastSeenPos;

    public float followDist;
    public WeaponStats weapon;
    Timer weaponTimer;

    public float maxHp;
    public float hp {get; set;}

    public float updateTime;

    float timeSeenPeer = 0f;
    float timeSeenPlayer = 0f;

    [Header("Sight Variables")]
    [Tooltip(@"The threshold for the enemy to count as ""seeing"" the player through noticing another enemy that does")]public float peerThreshold;
    [Tooltip("The threshold for seeing the player directly")]public float playerThreshold;
    [Tooltip("The max distance the enemy can see. The closer to this distance the player is, the worse it sees the player")]public float sightDist;
    [Tooltip("How fast the enemy should forget it saw something. Put as value / second")]public float memoryDeterioration;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        StartCoroutine(PathfindCoroutine());

        lastSeenPos = transform.position;

        weaponTimer = new(1f / weapon.fireRate);

        hp = maxHp;

        mask = LayerMask.GetMask("Player", "Map");
    }

    void OnValidate()
    {
        if(weapon && weapon.firingType != WeaponStats.FiringType.Hitscan)
        {
            Debug.LogError("Weapon needs to be hitscan!");
            weapon = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator PathfindCoroutine()
    {
        while (true)
        {            
            See();

            // if(pathfinding.path.Count > 0)
            //     transform.localScale = new(Mathf.Sign(pathfinding.path[0].x - transform.position.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y);

            // if (seesPlayer)
            // {
            //     lastSeenPos = Pathfinding.ClosestNode(EnemyPathfinding.pathGraph, player.position);
                
            //         if(Vector2.Distance(transform.position, player.position) > followDist) 
            //         pathfinding.Pathfind(player.position);
            //     else 
            //     {
            //         pathfinding.StopPathfinding();
            //         if (weaponTimer.finished)
            //         {
            //             ProjectileHandler.HitscanEnemy(transform.position, player.position, weapon);
            //             weaponTimer.ResetTimer();
            //         }
            //     }
            // }
            // else
            // {
            //     if (Vector2.Distance(transform.position, lastSeenPos) <= 0.1f)
            //     {
            //         lastSeenPos = new Vector3(-1f, -3f);
            //         pathfinding.Pathfind(lastSeenPos);
            //     }
            //     else pathfinding.Pathfind(lastSeenPos);
            // }

            yield return GetWaitForSeconds(updateTime);
        }
    }

    void See()
    {
        Vector3 playerScale = player.GetComponent<BoxCollider2D>().size * player.lossyScale / 2f + player.GetComponent<BoxCollider2D>().offset * player.lossyScale;
        visionCasts[0] = Physics2D.Linecast(transform.position, player.position, mask);
        visionCasts[1] = Physics2D.Linecast(transform.position, player.position + playerScale, mask);
        visionCasts[2] = Physics2D.Linecast(transform.position, player.position - playerScale, mask);
        visionCasts[3] = Physics2D.Linecast(transform.position, player.position + new Vector3(playerScale.x, -playerScale.y), mask);
        visionCasts[4] = Physics2D.Linecast(transform.position, player.position + new Vector3(-playerScale.x, playerScale.y), mask);

        peerVisionCasts = new();
        foreach(var obj in World.AllGameObjects(true, typeof(EnemyBehaviour)))
        {
            if(obj == gameObject) continue;
            peerVisionCasts.Add(Physics2D.Linecast(transform.position + (obj.transform.position - transform.position).normalized, obj.transform.position, LayerMask.GetMask("Enemy", "Map")));
        }
                
        int seeingPeers = Lists.ConditionCount(peerVisionCasts, x => {return x && x.collider.GetComponent<EnemyBehaviour>() && x.collider.GetComponent<EnemyBehaviour>().seesPlayer;});
        if(seeingPeers > 0)
            timeSeenPeer += updateTime * seeingPeers;
        else 
            timeSeenPeer -= updateTime * memoryDeterioration;
        timeSeenPeer = Mathf.Clamp(timeSeenPeer, 0f, Mathf.Infinity);
        
        bool knowsPlayerThroughPeer = timeSeenPeer > peerThreshold;


        int amtSeesPlayer = Lists.ConditionCount(visionCasts.ToList(), hit => {return hit.collider && hit.collider.transform == player;});

        Vector3 closestPoint = player.GetComponent<BoxCollider2D>().bounds.ClosestPoint(transform.position);
        float angle = Mathf.Abs(Angle2D.GetAngle(transform.position, closestPoint, 0f));
        float angleFactor = Mathf.Clamp01((90f - angle) / 90f);
        float distFactor = Mathf.Clamp01((sightDist - Vector2.Distance(transform.position, closestPoint)) / sightDist);
        float sightFactor = angleFactor * (amtSeesPlayer / 5f) * distFactor;
        timeSeenPlayer += updateTime * sightFactor;

        if(amtSeesPlayer > 0)
            timeSeenPlayer += updateTime * sightFactor;
        else
            timeSeenPlayer -= updateTime * memoryDeterioration;
        timeSeenPlayer = Mathf.Clamp(timeSeenPlayer, 0f, Mathf.Infinity);

        bool knowsPlayer = timeSeenPlayer > playerThreshold;

        seesPlayer = knowsPlayer || knowsPlayerThroughPeer;

        if(transform.name == "Enemy")
            // print($"Sees: {seesPlayer}, Knows: {knowsPlayer}, Peer: {knowsPlayerThroughPeer}");
            // print(sightFactor);
            print(sightFactor);
    }

    #pragma warning disable
    void OnDrawGizmos()
    {
        // return;
        foreach (var hit in visionCasts)
        {
            if (hit.collider && hit.collider.gameObject.name == "Player") Gizmos.color = Color.green;
            else Gizmos.color = Color.red;

            if(hit.collider)
                Gizmos.DrawLine(transform.position, hit.point);
        }

        foreach(var hit in peerVisionCasts)
        {
            if (hit.collider && hit.collider.CompareTag("Enemy")) Gizmos.color = Color.green;
            else Gizmos.color = Color.red;

            if(hit.collider)
                Gizmos.DrawLine(transform.position, hit.point);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(lastSeenPos, Vector3.one);
    }
}
