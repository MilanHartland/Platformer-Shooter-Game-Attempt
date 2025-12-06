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

    [HideInInspector]public bool seesPlayer;
    LayerMask mask;

    EnemyPathfinding pathfinding;

    Vector3 lastSeenPos;
    Vector3 startPos;
    Timer weaponTimer;

    Vector3 pfCenterRelative => Vector3.down * (1f / transform.lossyScale.y);
    Vector3 pfCenter => transform.position + pfCenterRelative;

    [Header("Fighting")]
    [Tooltip("The HP the enemy spawns with")]public float maxHp;
    [HideInInspector]public float hp;
    [Tooltip("The weapon the enemy uses. Has to be hitscan")]public WeaponStats weapon;
    [Tooltip("The distance the enemy follows to. When it comes to this distance, it stops pathfinding")]public float followDist;

    float playerKnowledge = 0f;

    [Header("Sight")]
    [Tooltip("How fast the sight and pathfinding update, in seconds")]public float updateTime;
    [Tooltip("The threshold the sight value needs before it counts as seeing the player")]public float sightThreshold;
    [Tooltip("The factor with which the sight is multiplied if this only sees a peer that knows the player"), Range(0f, 1f)]public float peerFactor;
    [Tooltip("The max distance the enemy can see. The closer to this distance the player is, the worse it sees the player")]public float sightDist;
    [Tooltip("How fast the enemy should forget it saw something. Put as value / second. For reference, the highest sight factor possible is 1 per second")]
    public float memoryDeterioration;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        StartCoroutine(PathfindCoroutine());

        startPos = lastSeenPos = transform.position;

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
            Visuals.Disintegrate(gameObject, dontThrowNonReadException: true);
        }
    }

    IEnumerator PathfindCoroutine()
    {
        while (true)
        {
            if(MenuManager.IsPaused) yield return null;

            See();

            if (seesPlayer)
            {
                lastSeenPos = Pathfinding.ClosestNode(EnemyPathfinding.pathGraph, player.position);
                
                    if(Vector2.Distance(pfCenter, player.position) > followDist) 
                    pathfinding.Pathfind(player.position);
                else 
                {
                    pathfinding.StopPathfinding();
                    if (weaponTimer.finished)
                    {
                        ProjectileHandler.HitscanEnemy(transform.Find("Gun").position, player.position, weapon);
                        weaponTimer.ResetTimer();
                    }
                }
            }
            else
            {
                if (Vector2.Distance(pfCenter, lastSeenPos) <= 0.1f)
                {
                    lastSeenPos = startPos;
                    pathfinding.Pathfind(startPos);
                }
                else pathfinding.Pathfind(lastSeenPos);
            }

            yield return GetWaitForSeconds(updateTime);
        }
    }

    void See()
    {
        bool seesPeer = false, seesDirect = true;

        //Gets every vision cast to the player and each corner of the collider
        Vector3 playerScale = player.GetComponent<BoxCollider2D>().size * player.lossyScale / 2f + player.GetComponent<BoxCollider2D>().offset * player.lossyScale;
        visionCasts[0] = Physics2D.Linecast(transform.position + Vector3.up * .5f, player.position, mask);
        visionCasts[1] = Physics2D.Linecast(transform.position + Vector3.up * .5f, player.position + playerScale, mask);
        visionCasts[2] = Physics2D.Linecast(transform.position + Vector3.up * .5f, player.position - playerScale, mask);
        visionCasts[3] = Physics2D.Linecast(transform.position + Vector3.up * .5f, player.position + new Vector3(playerScale.x, -playerScale.y), mask);
        visionCasts[4] = Physics2D.Linecast(transform.position + Vector3.up * .5f, player.position + new Vector3(-playerScale.x, playerScale.y), mask);

        //Gets every vision cast to the peers
        peerVisionCasts = new();
        foreach(var obj in World.AllGameObjects(true, typeof(EnemyBehaviour)))
        {
            if(obj == gameObject) continue;
            peerVisionCasts.Add(Physics2D.Linecast(transform.position + (obj.transform.position - transform.position).normalized, obj.transform.position, LayerMask.GetMask("Enemy", "Map")));
        }

        //For each peer vision cast, if it hits, hits a collider with EnemyBehaviour, and that peer seesPlayer, increase knowledge by distance * peerFactor
        float peerDebugLoggingValue = 0f;
        foreach(var obj in peerVisionCasts)
        {
            if(obj && obj.collider.GetComponent<EnemyBehaviour>() && obj.collider.GetComponent<EnemyBehaviour>().seesPlayer)
            {
                float peerDistFactor = Mathf.Clamp01((sightDist - Vector2.Distance(transform.position, obj.collider.transform.position)) / sightDist);
                playerKnowledge += updateTime * peerDistFactor * peerFactor;
                seesPeer = true;
                peerDebugLoggingValue += peerDistFactor * peerFactor;
            }
        }
        

        //Gets the amount of visioncasts that see the player
        int amtSeesPlayer = Lists.ConditionCount(visionCasts.ToList(), hit => {return hit.collider && hit.collider.transform == player;});

        //Gets the closestPoint so that the height of the player doesn't matter (seeing the feet or face of someone, in both cases you know someone is there equally)
        Vector3 closestPoint = player.GetComponent<BoxCollider2D>().bounds.ClosestPoint(transform.position);

        //Gets the angle, and inverts it if the enemy is looking left (lossyScale x = -1f)
        float angle = Mathf.Abs(Angle2D.GetAngle(transform.position, closestPoint, 0f));
        if(transform.lossyScale.x < 0f) angle = 180f - angle;

        //Gets the angle and distance factors, then calculates a sightFactor
        float angleFactor = Mathf.Clamp01((90f - angle) / 90f);
        float playerDistFactor = Mathf.Clamp01((sightDist - Vector2.Distance(transform.position, closestPoint)) / sightDist);
        float sightFactor = angleFactor * (amtSeesPlayer / 5f) * playerDistFactor;

        //Increases knowledge by sightFactor per second
        if(amtSeesPlayer > 0)
            playerKnowledge += updateTime * sightFactor;
        else seesDirect = false;

        //If there is no direct line to the player or a peer, decrease playerKnowledge by the memoryDeterioration
        if(!seesDirect && !seesPeer) playerKnowledge -= updateTime * memoryDeterioration;

        //Clamps knowledge so it's never below 0
        playerKnowledge = Mathf.Clamp(playerKnowledge, 0f, Mathf.Infinity);

        seesPlayer = playerKnowledge > sightThreshold && (seesDirect || seesPeer);

        // if(transform.name == "Enemy")
            // print($"SeesPlayer: {seesPlayer}, Knowledge: {playerKnowledge}, Sight: {sightFactor}, Peer: {peerDebugLoggingValue}");
    }

    #pragma warning disable
    void OnDrawGizmosSelected()
    {
        // return;
        foreach (var hit in visionCasts)
        {
            if (hit.collider && hit.collider.gameObject.name == "Player") Gizmos.color = Color.green;
            else Gizmos.color = Color.red;

            if(hit.collider)
                Gizmos.DrawLine(transform.position + Vector3.up * .5f, hit.point);
        }

        foreach(var hit in peerVisionCasts)
        {
            if (hit.collider && hit.collider.CompareTag("Enemy")) Gizmos.color = Color.green;
            else Gizmos.color = Color.red;

            if(hit.collider)
                Gizmos.DrawLine(transform.position, hit.point);
        }

        if(seesPlayer)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, Vector3.one * .5f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(lastSeenPos, Vector3.one);
    }
}
