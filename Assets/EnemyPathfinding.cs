using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MilanUtils;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPathfinding : MonoBehaviour
{
    public float speed;
    public float maxJumpHeight;

    Rigidbody2D rb;

    public static Dictionary<Vector3, List<Vector3>> pathGraph = new();
    [HideInInspector] public List<Vector3> path { get; private set; } = new();
    Vector3 curTarget = Vector3.zero;

    public Tilemap map;
    public Bounds floorBounds;
    bool grounded => Physics2D.OverlapBox(transform.position + floorBounds.center, floorBounds.size, 0f, mask);

    Pathfinding.ConnectionRequirements graphConnectionRequirements;

    LayerMask mask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GenerateMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) StartPathfindCoroutine(World.mousePos);
        if (Input.GetKeyDown(KeyCode.G)) pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
    }

    public void Pathfind(Vector3 target)
    {
        if (path.Count > 0) print(Lists.HasCondition(World.LinecastMultiple(transform.position, path[0], mask, Vector2.up), (hit) => { return !(RaycastHit2D)hit; }));
        if (path.Count < 1 || !Lists.HasCondition(World.LinecastMultiple(transform.position, path[0], mask, Vector2.up), (hit) => { return !(RaycastHit2D)hit; }))
        {
            path = Pathfinding.Dijkstra(transform.position, target, pathGraph);
            return;
        }

        List<Vector3> newPath = Pathfinding.Dijkstra(path[0], target, pathGraph);
        
        Vector3 firstCur = path[0];
        path = newPath;
        path.Insert(0, firstCur);

        StopAllCoroutines();
        StartPathfindCoroutine(target, false);
    }

    void StartPathfindCoroutine(Vector3 target, bool getPath = true) { StartCoroutine(PathfindCoroutine(target, getPath)); }
    IEnumerator PathfindCoroutine(Vector3 target, bool getPath = true)
    {
        //Sets the bool to true and gets the path
        if(getPath) path = Pathfinding.Dijkstra(transform.position, target, pathGraph);

        if(path.Count < 1){ print("NO PATH"); rb.linearVelocityX = 0f; yield break; }
        curTarget = path[0]; path.RemoveAt(0);

        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            if (Vector2.Distance(curTarget, transform.position) < 0.1f)
            {
                transform.position = curTarget;
                if (path.Count > 0) 
                {
                    curTarget = path[0]; 
                    path.RemoveAt(0); 
                }
                else { rb.linearVelocityX = 0f; yield break; }
            }

            Vector2 diff = curTarget - transform.position;

            if (diff.y > 0.05f && grounded) JumpTo(curTarget);

            if (diff.y > 0.05f && Physics2D.Linecast(transform.position, curTarget, mask))
                rb.linearVelocityX = 0f;
            else if (Mathf.Abs(diff.x) > speed * Time.fixedDeltaTime)
                rb.linearVelocityX = Mathf.Sign(diff.x) * speed;
            else if (Mathf.Abs(diff.x) < speed * Time.fixedDeltaTime)
            {
                rb.linearVelocityX = 0f;
                transform.position = new(curTarget.x, transform.position.y);
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public void JumpTo(Vector3 target)
    {
        Vector2 diff = target - transform.position;

        float g = -Physics2D.gravity.y;
        float fullTime = Mathf.Clamp(Mathf.Abs(diff.x), 0.2f, Mathf.Infinity) / speed;
        Vector2 vel = new(Mathf.Sign(diff.x) * speed, (diff.y+0.1f + 0.5f * g * fullTime * fullTime) / fullTime);

        rb.linearVelocity = vel;
    }

    [InspectorButton("Generate Map")]
    void GenerateMap()
    {
        mask = 1 << LayerMask.NameToLayer("Map");

        graphConnectionRequirements = (start, end) =>
        {
            bool clearLine = !Physics2D.Linecast(start, end, mask);
            Vector2 diff = end - start;
            bool corrY = diff.y <= maxJumpHeight;

            bool isAbove = diff.y < 0;
            bool canWalk = clearLine && diff.y == 0f;
            bool canFall = false;
            bool canJump = false;
            if (isAbove)
            {
                //Gets the linecast 1 unit to the left/right of the startk. If either is unobstructed, that means a fall is possible (disregarding x distance)
                bool lineLeft = !Physics2D.Linecast(start + Vector3.left * 0.5f, end, mask);
                bool lineRight = !Physics2D.Linecast(start + Vector3.right * 0.5f, end, mask);

                canFall = lineLeft || lineRight;
            }
            else
            {
                //Gets the height of the jump that would happen between start/end, and raycasts that distance up to see if it's unobstructed (then the jump would be possible if no map shenanigans)
                float fullTime = Mathf.Abs(diff.x) / speed;
                float height = (diff.y + 0.5f * -Physics2D.gravity.y * fullTime * fullTime) / fullTime;
                height = (height * height) / (Physics2D.gravity.y * -2f);
                corrY = height <= maxJumpHeight;
                bool nothingAbove = !Physics2D.Raycast(start, Vector2.up, Mathf.Clamp(height, 0f, Mathf.Infinity), mask);

                bool nothingInLine = true;
                for (int i = 0; i < Mathf.Abs(diff.x); i++)
                {
                    Vector3 signedDir = new(Mathf.Sign(diff.x), 0f);
                    if (!Physics2D.Raycast(start + i * signedDir, signedDir, 0f, mask))
                    {
                        if (Physics2D.Raycast(start + i * signedDir, Vector2.up, Mathf.Clamp(height, 0f, Mathf.Infinity), mask))
                        {
                            nothingInLine = false;
                            break;
                        }
                    }
                }

                //Gets the linecast 1 unit to the left/right of the end. If either is unobstructed, that means a jump is possible (disregarding x distance)
                bool lineLeft = !Physics2D.Linecast(end + Vector3.left * 0.5f, start, mask);
                bool lineRight = !Physics2D.Linecast(end + Vector3.right * 0.5f, start, mask);
                bool otherLine = lineLeft || lineRight;

                canJump = nothingAbove && nothingInLine && (clearLine || otherLine);
            }

            float fallX = Mathf.Abs(speed * Mathf.Sqrt(2f * Mathf.Abs(diff.y) / -Physics2D.gravity.y)) + 1.5f; //Gets the absolute of speed * falltime (sqrt of 2h/g). 1.5 to account for tile size
            float maxJumpTime = ((Mathf.Clamp(diff.y, 0f, Mathf.Infinity) + maxJumpHeight) / maxJumpHeight) * Mathf.Sqrt(2f * maxJumpHeight / -Physics2D.gravity.y);
            bool corrX = canFall ? (Mathf.Abs(diff.x) - 0.2f < fallX) : (Mathf.Abs(diff.x) < maxJumpTime * speed);

            return (canWalk || canJump || canFall) && corrX && corrY;
        };
        pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
    }
    
    #pragma warning disable
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        foreach (var obj in pathGraph.Keys)
        {
            Gizmos.DrawCube(obj, Vector3.one * 0.5f);
            foreach (var b in pathGraph[obj]) Gizmos.DrawLine(obj, b);
        }

        // return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(curTarget, Vector3.one);

        Gizmos.color = Color.blue;
        if (path.Count < 1) return;
        Gizmos.DrawLine(transform.position, curTarget); Gizmos.DrawLine(curTarget, path[0]);
        if (path.Count < 2) return;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
            Gizmos.DrawWireCube(path[i], 0.25f * Vector3.one);
        }
    }
}