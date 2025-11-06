using UnityEngine;
using MilanUtils;
using static MilanUtils.Objects;

[RequireComponent(typeof(EnemyPathfinding))]
public class EnemyBehaviour : MonoBehaviour
{
    RaycastHit2D[] visionCasts = new RaycastHit2D[4];
    bool seesPlayer;
    public LayerMask mask;

    EnemyPathfinding pathfinding;

    Vector3 lastSeenPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
    }

    // Update is called once per frame
    void Update()
    {
        See();

        if (seesPlayer)
        {
            lastSeenPos = Pathfinding.ClosestNode(EnemyPathfinding.pathGraph, player.position);
            pathfinding.Pathfind(player.position);
        }
        else
        {
            if (Vector2.Distance(transform.position, lastSeenPos) <= 0.1f)
            {
                lastSeenPos = Vector3.one;
                pathfinding.Pathfind(Vector3.one);
            }
            else pathfinding.Pathfind(lastSeenPos);
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
