using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MilanUtils;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BetterEnemyAI : MonoBehaviour
{
    public float speed;
    public float maxJumpHeight;

    Rigidbody2D rb;

    public static Dictionary<Vector3, List<Vector3>> pathGraph = new();
    List<Vector3> path = new();
    Vector3 curTarget = Vector3.zero;

    public Tilemap map;
    public Bounds floorBounds;
    bool grounded => Physics2D.OverlapBox(transform.position + floorBounds.center, floorBounds.size, 0f, ~(1 << gameObject.layer));

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Pathfinding.ConnectionRequirements requirements = (a, b) =>
        {
            Vector2 diff = b - a;
            bool corrX = Mathf.Abs(diff.x) <= 2f*speed - Mathf.Clamp(diff.y, 0f, maxJumpHeight);
            bool corrY = diff.y <= maxJumpHeight;

            return corrX && corrY;
        };
        pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, requirements, gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) Pathfind(World.mousePos);
    }

    void Pathfind(Vector3 target) { StartCoroutine(PathfindCoroutine(target)); }
    IEnumerator PathfindCoroutine(Vector3 target)
    {
        path = Pathfinding.Dijkstra(transform.position, target, pathGraph);

        if(path.Count < 2){ print("NO PATH"); yield break; }
        curTarget = path[0]; path.RemoveAt(0);
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            if (Vector2.Distance(curTarget, transform.position) <= 0.1f)
            {
                if (path.Count > 0) { curTarget = path[0]; path.RemoveAt(0); }
                else { rb.linearVelocityX = 0f; yield break; }
            }

            if (curTarget.y - transform.position.y > 0.05f && grounded) JumpTo(curTarget);
            rb.linearVelocityX = Mathf.Sign(curTarget.x - transform.position.x) * speed;

            yield return new WaitForFixedUpdate();
        }
    }

    public void JumpTo(Vector3 target)
    {
        Vector2 diff = target - transform.position;

        float g = -Physics2D.gravity.y;
        float fullTime = Mathf.Abs(diff.x) / speed;
        Vector2 vel = new(Mathf.Sign(diff.x) * speed, (diff.y + 0.5f * g * fullTime * fullTime) / fullTime);

        rb.linearVelocity = vel;
    }

    void OnDrawGizmos()
    {
        foreach (var obj in pathGraph.Keys)
        {
            Gizmos.DrawCube(obj, Vector3.one * 0.5f);
            foreach (var b in pathGraph[obj]) Gizmos.DrawLine(obj, b);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawCube(curTarget, Vector3.one);

        Gizmos.color = Color.blue;
        if (path.Count < 1) return;
        Gizmos.DrawLine(transform.position, curTarget); Gizmos.DrawLine(curTarget, path[0]);
        if (path.Count < 2) return;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }
}
