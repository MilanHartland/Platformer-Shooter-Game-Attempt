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

    Pathfinding.ConnectionRequirements graphConnectionRequirements;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // graphConnectionRequirements = (a, b) =>
        // {
        //     Vector2 diff = b - a;
        //     bool corrX = Mathf.Abs(diff.x) <= 2f*speed - Mathf.Clamp(diff.y, 0f, maxJumpHeight);
        //     bool corrY = diff.y <= maxJumpHeight;

        //     float fullTime = Mathf.Abs(diff.x) / speed;
        //     float height = (diff.y + 0.5f * -Physics2D.gravity.y * fullTime * fullTime) / fullTime;
        //     height = (height * height) / (Physics2D.gravity.y * -2f);
        //     bool cantJump = Physics2D.Raycast(a, Vector2.up, Mathf.Clamp(height, 0f, Mathf.Infinity), ~(1 << gameObject.layer));

        //     return corrX && corrY && !cantJump;
        // };
        // pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);

        GenerateMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) Pathfind(World.mousePos);
        if (Input.GetKeyDown(KeyCode.G)) pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
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

            if (Mathf.Abs(curTarget.x - transform.position.x) > speed * Time.fixedDeltaTime)
                rb.linearVelocityX = Mathf.Sign(curTarget.x - transform.position.x) * speed;
            else { rb.linearVelocityX = 0f; transform.position = new(curTarget.x, transform.position.y); }

            yield return new WaitForFixedUpdate();
        }
    }

    public void JumpTo(Vector3 target)
    {
        Vector2 diff = target - transform.position;

        float g = -Physics2D.gravity.y;
        float fullTime = Mathf.Abs(diff.x) / speed;
        Vector2 vel = new(Mathf.Sign(diff.x) * speed, (diff.y+0.1f + 0.5f * g * fullTime * fullTime) / fullTime);

        rb.linearVelocity = vel;
    }

    [InspectorButton("Generate Map")]
    void GenerateMap()
    {
        graphConnectionRequirements = (a, b) =>
        {
            bool clearLine = !Physics2D.Linecast(a, b, ~(1 << gameObject.layer));
            Vector2 diff = b - a;
            bool corrY = diff.y <= maxJumpHeight;

            float fullTime = Mathf.Abs(diff.x) / speed;
            float height = (diff.y + 0.5f * -Physics2D.gravity.y * fullTime * fullTime) / fullTime;
            height = (height * height) / (Physics2D.gravity.y * -2f);
            bool canJump = !Physics2D.Raycast(a, Vector2.up, Mathf.Clamp(height, 0f, Mathf.Infinity), ~(1 << gameObject.layer));

            bool isAbove = diff.y < 0;
            bool canFall = false;
            if (isAbove)
            {
                bool lineLeft = !Physics2D.Linecast(a + Vector3.left*0.5f, b);
                bool lineRight = !Physics2D.Linecast(a + Vector3.right*0.5f, b);

                canFall = (lineLeft || lineRight) && !clearLine;
            }

            float fallX = -1f * Mathf.Sign(diff.x) + Mathf.Abs(speed * Mathf.Sqrt(2f * Mathf.Abs(diff.y) / -Physics2D.gravity.y));
            float maxFallLength = ((Mathf.Clamp(diff.y, 0f, Mathf.Infinity) + maxJumpHeight) / maxJumpHeight) * Mathf.Sqrt(2f * maxJumpHeight / -Physics2D.gravity.y);
            bool corrX = canFall ? (Mathf.Abs(diff.x) < fallX) : (Mathf.Abs(diff.x) < maxFallLength * speed);
            
            return (clearLine || canFall) && corrX && corrY && canJump;
        };
        pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
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
