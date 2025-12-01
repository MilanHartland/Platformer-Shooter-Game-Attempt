using UnityEngine;
using MilanUtils;
using static MilanUtils.Variables;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyPathfinding))]
public class EnemyBehaviour : MonoBehaviour
{
    RaycastHit2D[] visionCasts = new RaycastHit2D[4];
    bool seesPlayer;
    public LayerMask mask;

    EnemyPathfinding pathfinding;

    Vector3 lastSeenPos;

    public float followDist;
    public WeaponStats weapon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        StartCoroutine(PathfindCoroutine());

        lastSeenPos = transform.position;
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
        
    }

    IEnumerator PathfindCoroutine()
    {
        while (true)
        {
            // pathfinding.Pathfind(World.mousePos);
            
            See();

            if (seesPlayer)
            {
                lastSeenPos = Pathfinding.ClosestNode(EnemyPathfinding.pathGraph, player.position);
                
                    if(Vector2.Distance(transform.position, player.position) > followDist) 
                    pathfinding.Pathfind(player.position);
                else 
                    pathfinding.StopPathfinding();
            }
            else
            {
                if (Vector2.Distance(transform.position, lastSeenPos) <= 0.1f)
                {
                    lastSeenPos = new Vector3(-1f, -3f);
                    pathfinding.Pathfind(lastSeenPos);
                }
                else pathfinding.Pathfind(lastSeenPos);
            }

            yield return GetWaitForSeconds(0.1f);
        }
    }

    void See()
    {
        Vector3 playerScale = player.lossyScale / 2f;
        visionCasts[0] = Physics2D.Linecast(transform.position, player.position + playerScale, mask);
        visionCasts[1] = Physics2D.Linecast(transform.position, player.position - playerScale, mask);
        visionCasts[2] = Physics2D.Linecast(transform.position, player.position + new Vector3(playerScale.x, -playerScale.y), mask);
        visionCasts[3] = Physics2D.Linecast(transform.position, player.position + new Vector3(-playerScale.x, playerScale.y), mask);

        foreach (var hit in visionCasts)
        {
            if (hit.collider && hit.collider.transform == player)
            {
                seesPlayer = true;
                return;
            }
        }
        seesPlayer = false;
    }

    #pragma warning disable
    void OnDrawGizmos()
    {
        // return;
        foreach (var hit in visionCasts)
        {
            if (hit.collider && hit.collider.gameObject.name == "Player") Gizmos.color = Color.green;
            else Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, hit.point);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(lastSeenPos, Vector3.one);
    }
}
