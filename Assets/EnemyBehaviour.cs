using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("Target Dummy")]
    [Tooltip("If this is a target dummy, without any AI")]public bool isDummy = false;

    RaycastHit2D[] visionCasts = new RaycastHit2D[5];
    List<RaycastHit2D> peerVisionCasts = new();

    [HideInInspector]public bool seesPlayer;
    LayerMask mask;

    EnemyPathfinding pathfinding;

    Vector3 lastSeenPos;
    Timer weaponTimer;

    Vector3 pfCenterRelative => Vector3.down * (1f / transform.lossyScale.y);
    Vector3 pfCenter => transform.position + pfCenterRelative;

    [Header("Fighting")]
    [Tooltip("The HP the enemy spawns with")]public float maxHp;
    [HideInInspector]public float hp;
    [Tooltip("The weapon the enemy uses. Has to be hitscan"), ShowIf("!isDummy")]public WeaponStats weapon;
    [Tooltip("The distance the enemy follows to. When it comes to this distance, it stops pathfinding"), ShowIf("!isDummy")]public float followDist;

    float playerKnowledge = 0f;

    [Header("Sight")]
    [Tooltip("How fast the sight and pathfinding update, in seconds"), ShowIf("!isDummy")]public float updateTime;
    [Tooltip("The threshold the sight value needs before it counts as seeing the player"), ShowIf("!isDummy")]public float sightThreshold;
    [Tooltip("The factor with which the sight is multiplied if this only sees a peer that knows the player"), ShowIf("!isDummy"), Range(0f, 1f)]public float peerFactor;
    [Tooltip("The max distance the enemy can see. The closer to this distance the player is, the worse it sees the player"), ShowIf("!isDummy")]public float sightDist;
    [Tooltip("How fast the enemy should forget it saw something. Put as value / second. For reference, the highest sight factor possible is 1 per second"), ShowIf("!isDummy")]
    public float memoryDeterioration;

    struct InfectionStats
    {
        public float damage;
        public float timeEnd;
        public readonly Timer timer;
        public InfectionStats(float dmg, float duration){damage = dmg; timeEnd = Time.time + duration; timer = new(1f);}
    }
    List<InfectionStats> infections = new();

    public Dictionary<string, float> info = new();

    [Header("Idle")]
    [Tooltip("A list of positions this enemy can wander to")]public List<Vector3> wanderPositions;
    [Tooltip("The amount of time there is between wanders"), ShowIf("!isDummy")]public FloatRange wanderTimeRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isDummy) weaponTimer = new(1f / weapon.fireRate);

        info.Add("Last Position", transform.position.x);
        info.Add("Cripple Damage", 0f);

        hp = maxHp;

        mask = LayerMask.GetMask("Player", "Map");

        if(TryGetComponent(out EnemyPathfinding pathf))
        {
            pathfinding = pathf;
            isDummy = false;
            StartCoroutine(PathfindCoroutine());
        }
        else isDummy = true;
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
        for(int i = infections.Count - 1; i >= 0; i--)
        {
            InfectionStats inf = infections[i];

            if(Time.time > inf.timeEnd)
            {
                infections.RemoveAt(0);
                continue;
            }
            else if (inf.timer)
            {
                TakeDamage(inf.damage);
                inf.timer.ResetTimer();
            }
        }

        if(!isDummy && hp <= 0)
        {
            Effects.Disintegrate(gameObject, dontThrowNonReadException: true);
        }
    }

    void FixedUpdate()
    {
        if (info.ContainsKey("Cripple"))
        {
            info["Cripple Damage"] += Mathf.Abs(transform.position.x - info["Last Position"]) * info["Cripple"];

            if(info["Cripple Damage"] > 1f)
            {
                TakeDamage(1f);
                info["Cripple Damage"]--;
            }
        }
        info["Last Position"] = transform.position.x;
    }

    IEnumerator SightCoroutine()
    {
        while (true)
        {
            if(MenuManager.IsPaused) yield return null;
            
            See();

            if(seesPlayer && Vector2.Distance(pfCenter, player.position) <= followDist)
            {
                pathfinding.StopPathfinding();
                if (weaponTimer.finished)
                {
                    Hitscan(transform.Find("Gun").position, player.position, weapon);
                    weaponTimer.ResetTimer();
                }
            }

            yield return null;
        }
    }

    IEnumerator PathfindCoroutine()
    {
        Timer wanderTime;
        Vector3 wanderPos;
        bool lookingForPlayer = false;
        
        StartCoroutine(SightCoroutine());
        wanderPos = wanderPositions[Random.Range(0, wanderPositions.Count)];
        wanderTime = new(wanderTimeRange.Random());

        while (true)
        {
            if(MenuManager.IsPaused) yield return null;

            //If sees the player, sets the last seen position to the closest node to the player. If the distance is over follow distance, pathfind towards player. Set lookingForPlayer to true
            if (seesPlayer)
            {
                lastSeenPos = Pathfinding.ClosestNode(EnemyPathfinding.pathGraph, player.position);
                
                if(Vector2.Distance(pfCenter, player.position) > followDist) 
                    pathfinding.Pathfind(player.position);
                
                lookingForPlayer = true;
            }
            //If doesn't see player, if distance to last seen position is close enough, stop looking and pathfind to wander position. Else, if looking, pathfind to last seen position
            else
            {
                if (Vector2.Distance(pfCenter, lastSeenPos) <= 0.1f)
                {
                    lookingForPlayer = false;
                    pathfinding.Pathfind(wanderPos);
                }
                else if(lookingForPlayer) pathfinding.Pathfind(lastSeenPos);
            }

            //If not looking for player, and thus wandering, decrease wander timer. If it is below 0, set a new wander position and reset timer. Then, pathfind to wander pos
            if (!lookingForPlayer)
            {
                if(wanderTime)
                {
                    wanderPos = wanderPositions[Random.Range(0, wanderPositions.Count)];

                    wanderTime = new(wanderTimeRange.Random());
                }

                pathfinding.Pathfind(wanderPos);
            }
            
            //If there is a path, save the current target node. While pathfinding and the path is longer than 0 and the target pos is the same as previous target node, skip to next frame
            if(pathfinding.path.Count > 0)
            {
                Vector3 curPath0 = pathfinding.path[0];
                while(pathfinding.isPathfinding && pathfinding.path.Count > 0 && pathfinding.path[0] == curPath0) 
                    yield return new WaitForFixedUpdate();
            }
            yield return new WaitForFixedUpdate();
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
        int amtSeesPlayer = visionCasts.ToList().Count(hit => {return hit.collider && hit.collider.transform == player;});

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

    void Hitscan(Vector3 pos, Vector3 target, WeaponStats w)
    {
        Vector3 angle = Angle2D.GetAngle<Vector2>(pos, target, -90f + Random.Range(-w.spread / 2f, w.spread / 2f));
        RaycastHit2D hit = Physics2D.Raycast(pos, angle, w.maxHitscanDistance, ~LayerMask.GetMask("Enemy"));

        Vector3 endPos = hit ? hit.point : pos + angle * w.maxHitscanDistance;
        Effects.SpawnLine(new(){pos, endPos}, Color.yellow, .05f, .1f);

        if(hit.collider.TryGetComponent(out PlayerManager pm)) pm.hp -= w.damage;
        FloatingText.SpawnDamageText(hit.collider.gameObject, w.damage);
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        playerKnowledge = Mathf.Max(playerKnowledge, .8f * sightThreshold);
        FloatingText.SpawnDamageText(gameObject, damage);
    }

    public void ApplyInfection(float damage, float seconds){infections.Add(new(damage, seconds));}

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

        foreach(var pos in wanderPositions)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos, Vector3.one);
        }
    }
}
