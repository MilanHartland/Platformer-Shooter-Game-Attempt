using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MilanUtils;
using static MilanUtils.Trajectory;
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
        if (Input.GetKeyDown(KeyCode.X)) rb.linearVelocity = GetJumpToConstantSpeed(transform.position, curTarget, speed);
        if (Input.GetKeyDown(KeyCode.G)) pathGraph = Pathfinding.GenerateMapDijkstraGraphFull(map, true, graphConnectionRequirements, gameObject);
    }

    public void Pathfind(Vector3 target)
    {
        //If no path, or there is no unobstructed line in LinecastMultiple (in this case a linecast from this position and 1 higher towards the path), reset path
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
        if (getPath) path = Pathfinding.Dijkstra(transform.position, target, pathGraph);

        if (path.Count < 1) { Debug.Log("NO PATH"); rb.linearVelocityX = 0f; yield break; }
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

            if (diff.y > 0.05f)
            {
                if (grounded)
                    rb.linearVelocity = GetJumpToConstantSpeed(transform.position, curTarget, speed);

                if (Physics2D.Linecast(transform.position, curTarget, mask) && Mathf.Abs(diff.x) < 1.05f)
                    rb.linearVelocityX = 0f;
            }
            else
            {
                if (grounded && !Physics2D.Raycast(transform.position + new Vector3(Mathf.Sign(diff.x), 0f), Vector2.down, 1f, mask) && Mathf.Abs(diff.x) > 0.45f)
                    rb.linearVelocity = GetJumpToConstantSpeed(transform.position, curTarget, speed);
                else if (Mathf.Abs(diff.x) > speed * Time.fixedDeltaTime)
                    rb.linearVelocityX = Mathf.Sign(diff.x) * speed;
                else if (Mathf.Abs(diff.x) < speed * Time.fixedDeltaTime)
                {
                    rb.linearVelocityX = 0f;
                    transform.position = new(curTarget.x, transform.position.y);
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    float GetJumpVelocity(Vector3 start, Vector3 target)
    {
        Vector2 diff = target - start;

        float g = -Physics2D.gravity.y;
        float fullTime = Mathf.Abs(diff.x) / speed;
        return (diff.y + 0.1f + g * 0.5f * fullTime * fullTime) / fullTime;
    }
    
    float GetJumpHeight(Vector3 start, Vector3 target)
    {
        float vel = GetJumpVelocity(start, target);
        return (0.5f * vel * vel) / -Physics2D.gravity.y;
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
            bool isSame = Mathf.Abs(diff.y) < 0.05f;
            bool canWalk = false;
            bool canFall = false;
            bool canJump = false;
            if (isAbove)
            {
                //Gets the linecast 1 unit to the left/right of the startk. If either is unobstructed, that means a fall is possible (disregarding x distance)
                bool lineLeft = !Physics2D.Linecast(start + Vector3.left * 0.5f, end, mask);
                bool lineRight = !Physics2D.Linecast(start + Vector3.right * 0.5f, end, mask);

                canFall = lineLeft || lineRight;
            }
            else if (isSame)
            {
                //Checks if there is a gap by doing a raycastline downward and returning if on any there is no hit
                List<RaycastHit2D> hitList = World.RaycastLine(start, new(Mathf.Sign(diff.x), 0f), Mathf.RoundToInt(Mathf.Abs(diff.x) - 1), Vector2.down, mask);
                bool hasGap = Lists.HasCondition(hitList, (hit) => { return !(RaycastHit2D)hit; });

                //If there is a gap, set canWalk to false, check if jump is allowed (GetJumpRayList and HasCondition), then set canJump. If no gap, then canWalk is true
                if (hasGap)
                {
                    canWalk = false;
                    corrY = GetJumpHeight(start, end) <= maxJumpHeight;

                    hitList = GetJumpRayList();
                    bool nothingInLine = !Lists.HasCondition(hitList, (hit) => { return (RaycastHit2D)hit; });

                    canJump = nothingInLine && clearLine;
                }
                else canWalk = clearLine;
            }
            else
            {
                //Gets the height of the jump that would happen between start/end and check if maxJumpHeight allows it
                corrY = GetJumpHeight(start, end) <= maxJumpHeight;

                //Checks if jump is possible
                var hitList = GetJumpRayList();
                bool nothingInLine = !Lists.HasCondition(hitList, (hit) => { return (RaycastHit2D)hit; });

                //Gets the linecast 1 units to the left/right of the end. If either is unobstructed, that means a jump is possible (disregarding x distance)
                bool lineLeft = !Physics2D.Linecast(end + Vector3.left * 1f, start, mask);
                bool lineRight = !Physics2D.Linecast(end + Vector3.right * 1f, start, mask);
                bool otherLine = lineLeft || lineRight;

                canJump = nothingInLine && (clearLine || otherLine);
            }
            
            bool corrFallX = Mathf.Abs(diff.x) - 0.2f < GetTimeToFall(Mathf.Abs(diff.y)) * speed;

            float maxJumpTime = 2f * GetJumpHeight(start, end) / speed;
            bool corrJumpX = Mathf.Abs(diff.x) < maxJumpTime * speed;

            bool corrX = (canFall && corrFallX) || (canJump && corrJumpX) || canWalk;

            if(start == new Vector3(-2, 1) && end == new Vector3(-6, 1)) print(corrJumpX);

            return (canWalk || canJump || canFall) && corrX && corrY;

            List<RaycastHit2D> GetJumpRayList()
            {
                float height = GetJumpHeight(start, end);

                //Gets the raycast variables
                Vector2 rayStart = start + new Vector3(0f, transform.lossyScale.y * .5f);
                Vector2 moveDir = new(Mathf.Sign(diff.x), 0f);
                int moveCount = Mathf.RoundToInt(Mathf.Abs(diff.x) - 1);

                //For each move to be made, add 2 things: a raycast from (start + move i steps) up to the height of the jump, and a linecast from that height to next height
                List<RaycastHit2D> hitList = new();
                for (int i = 0; i <= moveCount; i++)
                {
                    float heightChange = GetHeightChangeAfterTime(GetJumpVelocity(start, end), (float)i / speed);
                    float heightChangeNext = GetHeightChangeAfterTime(GetJumpVelocity(start, end), (float)(i + 1) / speed);

                    hitList.Add(Physics2D.Raycast(rayStart + moveDir * i, Vector2.up, heightChange, mask));
                    hitList.Add(Physics2D.Linecast(rayStart + moveDir * i + Vector2.up * heightChange, rayStart + moveDir * i + Vector2.up * heightChangeNext, mask));
                }
                
                return hitList;
            }
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
            
            foreach (var b in pathGraph[obj])
            {
                bool twoWay = pathGraph[b].Contains(obj);
                if (twoWay) Gizmos.color = Color.white;
                else if (obj.y > b.y) Gizmos.color = new Color(.7f, 1f, .7f, 1f);
                else if (obj.y < b.y) Gizmos.color = new Color(.7f, .7f, 1f, 1f);

                Gizmos.DrawLine(obj, b);
            }
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