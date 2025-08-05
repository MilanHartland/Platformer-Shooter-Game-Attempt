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

    public Tilemap map;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (pathGraph.Count == 0) pathGraph = Pathfinding.GenerateDijkstraGraph(map);
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) path = Pathfinding.Dijkstra(transform.position, World.mousePos, pathGraph);
    }

    void Pathfind(Vector3 target) { StartCoroutine(PathfindCoroutine(target)); }
    IEnumerator PathfindCoroutine(Vector3 target)
    {
        Vector3 curTarget = target;
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            RaycastHit2D lineObstructed = Physics2D.Linecast(transform.position, target, ~(1 << gameObject.layer));
            if (lineObstructed)
            {

            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void JumpTo(Vector3 target)
    {
        //MAKE THE ENEMY CALCULATE THE VELOCITY OR HEIGHT FOR THE JUMP WHEN MOVING AT CONSTANT SPEED
        //ALTERNATIVELY CHEAT IT SOMEHOW (then the problem'd be how to cheat lol)

        Vector2 diff = target - transform.position;

        float g = -Physics2D.gravity.y;
        float fullTime = Mathf.Abs(diff.x) / speed;
        Vector2 vel = new(Mathf.Sign(diff.x) * speed, (diff.y + 0.5f * g * fullTime * fullTime) / fullTime);

        rb.linearVelocity = vel;
    }

    Vector3 GetClosestNode()
    {
        float min = Mathf.Infinity; Vector3 closest = Vector3.positiveInfinity;
        foreach (Vector3 vec in pathGraph.Keys) { if (Vector2.Distance(transform.position, vec) <= min) { closest = vec; } }
        return closest;
    }

    void OnDrawGizmos()
    {
        // foreach (var obj in pathGraph)
        // {
        //     foreach (var listObj in obj.Value)
        //     {
        //         Gizmos.DrawLine(obj.Key, listObj);
        //     }
        // }

        foreach (var obj in pathGraph.Keys) { Gizmos.DrawCube(obj, Vector3.one * 0.5f); }

        if (path.Count < 2) return;
        for (int i = 0; i < path.Count; i++)
        {
            if (i < path.Count - 1) Gizmos.DrawLine(path[i], path[i + 1]);
        }
        Gizmos.DrawLine(transform.position, path[0]); Gizmos.DrawLine(World.mousePos, path[^1]);
    }
}
